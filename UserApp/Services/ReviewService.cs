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

public class ReviewService : IReviewService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _appDbContextFactory;
    private readonly Func<LogDbContext> _logDbContextFactory;
    private readonly Func<MessageFactory> _messageFactoryFactory;

    public ReviewService(
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

    public async Task<List<ReviewableSubmissionModel>> GetReviewableSubmissionsAsync()
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var messageFactory = _messageFactoryFactory();
        var paperMessages = messageFactory.GetPaperAndRandomnessMessagesAsync(sharedKey);
        var models = new List<ReviewableSubmissionModel>();

        await foreach (var paperMessage in paperMessages)
        {
            var paperHash = await Task.Run(() => SHA256.HashData(paperMessage.Paper));
            var hasReview = await appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.Review
                && @event.Identifier == paperHash
                && @event.UserId == _sessionService.UserId!.Value
            );

            if (hasReview)
            {
                continue;
            }

            var model = new ReviewableSubmissionModel(paperHash);
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
        var paperMessage = await messageFactory.GetPaperAndRandomnessMessageByPaperHashAsync(paperHash, sharedKey);
        await File.WriteAllBytesAsync(paperFilePath, paperMessage.Paper);
    }

    public async Task SendReviewAsync(byte[] paperHash, string review)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedPrivateKey);
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var messageFactory = _messageFactoryFactory();
        var paperMessage = await messageFactory.GetPaperAndRandomnessMessageByPaperHashAsync(paperHash, sharedKey);

        var reviewRandomness = new BigInteger(paperMessage.ReviewRandomness);
        var reviewCommitment = Commitment.Create(paperMessage.Paper, reviewRandomness);
        var matchingMessage = await messageFactory.GetMatchingMessageByCommitmentAsync(reviewCommitment.ToBytes());

        var reviewMessage = new ReviewMessage { Paper = paperMessage.Paper, Review = Encoding.UTF8.GetBytes(review) };
        var signatureMessage = new ReviewCommitmentAndNonceSignatureMessage
        {
            ReviewCommitment = matchingMessage.ReviewCommitment,
            ReviewNonce = matchingMessage.ReviewNonce
        };

        var reviewEntry = new LogEntry
        {
            Step = ProtocolStep.Review,
            Data = await reviewMessage.SerializeAsync(privateKey, sharedKey)
        };
        var signatureEntry = new LogEntry
        {
            Step = ProtocolStep.ReviewCommitmentAndNonceSignature,
            Data = await signatureMessage.SerializeAsync(privateKey)
        };

        await using var logDbContext = _logDbContextFactory();
        logDbContext.Entries.Add(reviewEntry);
        logDbContext.Entries.Add(signatureEntry);

        var logEvent = new LogEvent
        {
            Step = reviewEntry.Step,
            Identifier = paperHash,
            UserId = _sessionService.UserId!.Value
        };
        appDbContext.LogEvents.Add(logEvent);

        await logDbContext.SaveChangesAsync();
        await appDbContext.SaveChangesAsync();
    }
}
