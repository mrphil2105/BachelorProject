using System.Security.Cryptography;
using System.Text;
using Apachi.Shared;
using Apachi.Shared.Data;
using Apachi.Shared.Factories;
using Apachi.Shared.Messages;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;

namespace Apachi.UserApp.Services;

public class DiscussionService : IDiscussionService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _appDbContextFactory;
    private readonly Func<LogDbContext> _logDbContextFactory;
    private readonly Func<MessageFactory> _messageFactoryFactory;

    public DiscussionService(
        ISessionService sessionService,
        Func<AppDbContext> appDbContextFactory,
        Func<LogDbContext> logDbContextFactory,
        Func<MessageFactory> messageFactoryFactory
    )
    {
        _sessionService = sessionService;
        _appDbContextFactory = appDbContextFactory;
        _logDbContextFactory = logDbContextFactory;
        _messageFactoryFactory = messageFactoryFactory;
    }

    public async Task<List<DiscussableSubmissionModel>> GetDiscussableSubmissionsAsync()
    {
        var models = new List<DiscussableSubmissionModel>();

        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var logDbContext = _logDbContextFactory();
        await using var messageFactory = _messageFactoryFactory();

        var reviewsEntries = await logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.ReviewsShare)
            .ToListAsync();
        var groupKeyMessages = messageFactory.GetGroupKeyAndRandomnessMessagesAsync(sharedKey);

        await foreach (var groupKeyMessage in groupKeyMessages)
        {
            var matchingMessage = await messageFactory.GetMatchingMessageByPaperAsync(groupKeyMessage.Paper, sharedKey);

            foreach (var reviewsEntry in reviewsEntries)
            {
                ReviewsShareMessage reviewsMessage;

                try
                {
                    reviewsMessage = await ReviewsShareMessage.DeserializeAsync(
                        reviewsEntry.Data,
                        groupKeyMessage.GroupKey,
                        matchingMessage.ReviewerPublicKeys
                    );
                }
                catch (CryptographicException)
                {
                    continue;
                }

                var reviewModels = new List<DiscussReviewModel>();

                for (var i = 0; i < reviewsMessage.Reviews.Count; i++)
                {
                    // TODO: Use the hash array directly in the model and create an IValueConverter that displays longer
                    // hexes and another that displays short hexes. The former can be used for paper hashes and latter
                    // for reviewer public key hashes.
                    var publicKeyHash = await Task.Run(() => SHA256.HashData(matchingMessage.ReviewerPublicKeys[i]));
                    var hashString = Convert.ToHexString(publicKeyHash).Remove(10);
                    var review = Encoding.UTF8.GetString(reviewsMessage.Reviews[i]);

                    var reviewModel = new DiscussReviewModel(hashString, review);
                    reviewModels.Add(reviewModel);
                }

                var paperHash = await Task.Run(() => SHA256.HashData(groupKeyMessage.Paper));
                var model = new DiscussableSubmissionModel(paperHash, reviewModels, reviewsEntry.CreatedDate);
                models.Add(model);
            }
        }

        return models;
    }

    public async Task DownloadPaperAsync(byte[] paperHash, string paperFilePath)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var logDbContext = _logDbContextFactory();
        var paperEntries = logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.PaperShare)
            .AsAsyncEnumerable();

        await foreach (var paperEntry in paperEntries)
        {
            PaperShareMessage paperMessage;

            try
            {
                paperMessage = await PaperShareMessage.DeserializeAsync(paperEntry.Data, sharedKey);
            }
            catch (CryptographicException)
            {
                continue;
            }

            var sharePaperHash = await Task.Run(() => SHA256.HashData(paperMessage.Paper));

            if (!sharePaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            await File.WriteAllBytesAsync(paperFilePath, paperMessage.Paper);
            return;
        }

        throw new InvalidOperationException(
            $"A matching {ProtocolStep.PaperShare} entry for the paper hash was not found."
        );
    }

    public async Task SendMessageAsync(byte[] paperHash, string message)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedPrivateKey);
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var messageFactory = _messageFactoryFactory();
        var groupKeyMessage = await messageFactory.GetGroupKeyAndRandomnessMessageByPaperHashAsync(
            paperHash,
            sharedKey
        );

        await using var logDbContext = _logDbContextFactory();
        var discussionMessage = new DiscussionMessage { Message = Encoding.UTF8.GetBytes(message) };
        var messageEntry = new LogEntry
        {
            Step = ProtocolStep.Discussion,
            Data = await discussionMessage.SerializeAsync(privateKey, groupKeyMessage.GroupKey)
        };
        logDbContext.Entries.Add(messageEntry);

        var logEvent = new LogEvent
        {
            Step = messageEntry.Step,
            Identifier = paperHash,
            ReviewerId = _sessionService.UserId!.Value
        };
        appDbContext.LogEvents.Add(logEvent);

        await logDbContext.SaveChangesAsync();
        await appDbContext.SaveChangesAsync();
    }

    public async Task<List<DiscussMessageModel>> GetMessagesAsync(byte[] paperHash)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedPrivateKey);
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var logDbContext = _logDbContextFactory();
        await using var messageFactory = _messageFactoryFactory();

        var groupKeyMessage = await messageFactory.GetGroupKeyAndRandomnessMessageByPaperHashAsync(
            paperHash,
            sharedKey
        );
        var matchingMessage = await messageFactory.GetMatchingMessageByPaperAsync(groupKeyMessage.Paper, sharedKey);

        var discussionEntries = await logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.Discussion)
            .ToListAsync();
        var messageModels = new List<DiscussMessageModel>();

        foreach (var discussionEntry in discussionEntries)
        {
            foreach (var reviewerPublicKey in matchingMessage.ReviewerPublicKeys)
            {
                DiscussionMessage discussionMessage;

                try
                {
                    discussionMessage = await DiscussionMessage.DeserializeAsync(
                        discussionEntry.Data,
                        groupKeyMessage.GroupKey,
                        reviewerPublicKey
                    );
                }
                catch (CryptographicException)
                {
                    continue;
                }

                var publicKeyHash = await Task.Run(() => SHA256.HashData(reviewerPublicKey));
                var hashString = Convert.ToHexString(publicKeyHash).Remove(10);
                var message = Encoding.UTF8.GetString(discussionMessage.Message);
                var messageModel = new DiscussMessageModel(hashString, message);
                messageModels.Add(messageModel);
            }
        }

        return messageModels;
    }
}
