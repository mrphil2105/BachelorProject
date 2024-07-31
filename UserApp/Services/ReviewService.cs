using System.Security.Cryptography;
using System.Text;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
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

    public ReviewService(
        ISessionService sessionService,
        Func<AppDbContext> appDbContextFactory,
        Func<LogDbContext> logDbContextFactory
    )
    {
        _sessionService = sessionService;
        _appDbContextFactory = appDbContextFactory;
        _logDbContextFactory = logDbContextFactory;
    }

    public async Task<List<ReviewableSubmissionModel>> GetReviewableSubmissionsAsync()
    {
        var models = new List<ReviewableSubmissionModel>();

        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var logDbContext = _logDbContextFactory();
        var shareEntries = logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.PaperAndReviewRandomnessReviewerShare)
            .AsAsyncEnumerable();

        await foreach (var shareEntry in shareEntries)
        {
            PaperAndReviewRandomnessReviewerShareMessage shareMessage;

            try
            {
                shareMessage = await PaperAndReviewRandomnessReviewerShareMessage.DeserializeAsync(
                    shareEntry.Data,
                    sharedKey
                );
            }
            catch (CryptographicException)
            {
                continue;
            }

            var paperHash = await Task.Run(() => SHA256.HashData(shareMessage.Paper));
            var hasReview = await appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.Review
                && @event.Identifier == paperHash
                && @event.ReviewerId == _sessionService.UserId!.Value
            );

            if (hasReview)
            {
                continue;
            }

            var model = new ReviewableSubmissionModel(shareEntry.Id, shareEntry.CreatedDate);
            models.Add(model);
        }

        return models;
    }

    public async Task DownloadPaperAsync(Guid logEntryId, string paperFilePath)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var logDbContext = _logDbContextFactory();
        var shareEntry = await logDbContext.Entries.FirstAsync(entry => entry.Id == logEntryId);
        var shareMessage = await PaperAndReviewRandomnessReviewerShareMessage.DeserializeAsync(
            shareEntry.Data,
            sharedKey
        );

        await File.WriteAllBytesAsync(paperFilePath, shareMessage.Paper);
    }

    public async Task SendReviewAsync(Guid logEntryId, string review)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedPrivateKey);
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var logDbContext = _logDbContextFactory();
        var shareEntry = await logDbContext.Entries.SingleAsync(entry => entry.Id == logEntryId);
        var shareMessage = await PaperAndReviewRandomnessReviewerShareMessage.DeserializeAsync(
            shareEntry.Data,
            sharedKey
        );
        var matchingMessage = await FindMatchingMessageAsync(shareMessage, logDbContext);

        var reviewMessage = new ReviewMessage { Review = Encoding.UTF8.GetBytes(review) };
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
        logDbContext.Entries.Add(reviewEntry);
        logDbContext.Entries.Add(signatureEntry);

        var paperHash = await Task.Run(() => SHA256.HashData(shareMessage.Paper));
        var logEvent = new LogEvent
        {
            Step = reviewEntry.Step,
            Identifier = paperHash,
            ReviewerId = _sessionService.UserId!.Value
        };
        appDbContext.LogEvents.Add(logEvent);

        await logDbContext.SaveChangesAsync();
        await appDbContext.SaveChangesAsync();
    }

    private async Task<PaperReviewersMatchingMessage> FindMatchingMessageAsync(
        PaperAndReviewRandomnessReviewerShareMessage shareMessage,
        LogDbContext logDbContext
    )
    {
        var reviewRandomness = new BigInteger(shareMessage.ReviewRandomness);
        var reviewCommitment = Commitment.Create(shareMessage.Paper, reviewRandomness);
        var reviewCommitmentBytes = reviewCommitment.ToBytes();

        var matchingEntries = logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.PaperReviewersMatching)
            .AsAsyncEnumerable();

        await foreach (var matchingEntry in matchingEntries)
        {
            var matchingMessage = await PaperReviewersMatchingMessage.DeserializeAsync(matchingEntry.Data);

            if (matchingMessage.ReviewCommitment.SequenceEqual(reviewCommitmentBytes))
            {
                return matchingMessage;
            }
        }

        throw new InvalidOperationException(
            $"A matching {ProtocolStep.PaperReviewersMatching} entry for the "
                + $"{ProtocolStep.PaperAndReviewRandomnessReviewerShare} entry was not found."
        );
    }
}
