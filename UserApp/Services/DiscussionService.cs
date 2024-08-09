using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Factories;
using Apachi.Shared.Messages;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;

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
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var messageFactory = _messageFactoryFactory();
        var groupKeyMessages = messageFactory.GetGroupKeyAndRandomnessMessagesAsync(sharedKey);
        var models = new List<DiscussableSubmissionModel>();

        await foreach (var groupKeyMessage in groupKeyMessages)
        {
            var paperHash = await Task.Run(() => SHA256.HashData(groupKeyMessage.Paper));
            var hasGrade = await appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.Grade
                && @event.Identifier == paperHash
                && @event.ReviewerId == _sessionService.UserId!.Value
            );

            if (hasGrade)
            {
                continue;
            }

            var matchingMessage = await messageFactory.GetMatchingMessageByPaperAsync(groupKeyMessage.Paper, sharedKey);
            var reviewsMessage = await messageFactory.GetReviewsMessageByGroupKeyAsync(
                groupKeyMessage.GroupKey,
                matchingMessage.ReviewerPublicKeys
            );

            if (reviewsMessage == null)
            {
                continue;
            }

            var reviewModels = new List<ReviewModel>();

            for (var i = 0; i < reviewsMessage.Reviews.Count; i++)
            {
                var publicKeyHash = await Task.Run(() => SHA256.HashData(matchingMessage.ReviewerPublicKeys[i]));
                var hashString = Convert.ToHexString(publicKeyHash).Remove(10);
                var review = Encoding.UTF8.GetString(reviewsMessage.Reviews[i]);

                var reviewModel = new ReviewModel(hashString, review);
                reviewModels.Add(reviewModel);
            }

            var model = new DiscussableSubmissionModel { PaperHash = paperHash, Reviews = reviewModels };
            models.Add(model);
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

        await using var messageFactory = _messageFactoryFactory();
        var paperMessage = await messageFactory.GetPaperMessageByPaperHashAsync(paperHash, sharedKey);
        await File.WriteAllBytesAsync(paperFilePath, paperMessage.Paper);
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

        var discussionMessagesAndPublicKeys = messageFactory.GetDiscussionMessagesByGroupKeyAsync(
            groupKeyMessage.GroupKey,
            matchingMessage.ReviewerPublicKeys
        );
        var messageModels = new List<DiscussMessageModel>();

        await foreach (var (discussionMessage, reviewerPublicKey) in discussionMessagesAndPublicKeys)
        {
            var publicKeyHash = await Task.Run(() => SHA256.HashData(reviewerPublicKey));
            var message = Encoding.UTF8.GetString(discussionMessage.Message);
            var messageModel = new DiscussMessageModel(publicKeyHash, message);
            messageModels.Add(messageModel);
        }

        return messageModels;
    }

    public async Task SendGradeAsync(byte[] paperHash, int grade)
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
        var matchingMessage = await messageFactory.GetMatchingMessageByPaperAsync(groupKeyMessage.Paper, sharedKey);

        var gradeRandomness = new BigInteger(groupKeyMessage.GradeRandomness);
        var gradeNonce = GenerateBigInteger().ToByteArray();
        var gradeAndNonce = SerializeGrade(grade, gradeNonce);
        var gradeCommitment = Commitment.Create(gradeAndNonce, gradeRandomness);

        var signatureMessage = new GradeCommitmentsAndNonceSignatureMessage
        {
            ReviewCommitment = matchingMessage.ReviewCommitment,
            GradeCommitment = gradeCommitment.ToBytes(),
            ReviewNonce = matchingMessage.ReviewNonce
        };
        var gradeMessage = new GradeMessage { Grade = gradeAndNonce };

        var signatureEntry = new LogEntry
        {
            Step = ProtocolStep.GradeCommitmentsAndNonceSignature,
            Data = await signatureMessage.SerializeAsync(privateKey)
        };
        var gradeEntry = new LogEntry
        {
            Step = ProtocolStep.Grade,
            Data = await gradeMessage.SerializeAsync(privateKey, groupKeyMessage.GroupKey)
        };

        await using var logDbContext = _logDbContextFactory();
        logDbContext.Entries.Add(signatureEntry);
        logDbContext.Entries.Add(gradeEntry);

        var logEvent = new LogEvent
        {
            Step = gradeEntry.Step,
            Identifier = paperHash,
            ReviewerId = _sessionService.UserId!.Value
        };
        appDbContext.LogEvents.Add(logEvent);

        await logDbContext.SaveChangesAsync();
        await appDbContext.SaveChangesAsync();
    }
}
