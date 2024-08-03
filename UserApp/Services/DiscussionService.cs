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

public class DiscussionService : IDiscussionService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _appDbContextFactory;
    private readonly Func<LogDbContext> _logDbContextFactory;

    public DiscussionService(
        ISessionService sessionService,
        Func<AppDbContext> appDbContextFactory,
        Func<LogDbContext> logDbContextFactory
    )
    {
        _sessionService = sessionService;
        _appDbContextFactory = appDbContextFactory;
        _logDbContextFactory = logDbContextFactory;
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
        var groupKeyEntries = await logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.GroupKeyAndGradeRandomnessShare)
            .ToListAsync();
        var reviewsEntries = await logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.ReviewsShare)
            .ToListAsync();

        foreach (var reviewsEntry in reviewsEntries)
        {
            var (reviewsMessage, matchingMessage, groupKeyMessage) = await DeserializeReviewsMessageAsync(
                reviewsEntry,
                groupKeyEntries,
                sharedKey
            );
            var reviewModels = new List<DiscussReviewModel>();

            for (var i = 0; i < reviewsMessage.Reviews.Count; i++)
            {
                var publicKey = matchingMessage.ReviewerPublicKeys[i];
                var publicKeyHash = await Task.Run(() => SHA256.HashData(publicKey));
                var hashString = Convert.ToHexString(publicKeyHash).Remove(8);
                var review = Encoding.UTF8.GetString(reviewsMessage.Reviews[i]);
                var reviewModel = new DiscussReviewModel(hashString, review);
                reviewModels.Add(reviewModel);
            }

            var paperHash = await Task.Run(() => SHA256.HashData(groupKeyMessage.Paper));
            var model = new DiscussableSubmissionModel(paperHash, reviewModels, reviewsEntry.CreatedDate);
            models.Add(model);
        }

        return models;
    }

    private async Task<(
        ReviewsShareMessage ReviewsMessage,
        PaperReviewersMatchingMessage MatchingMessage,
        GroupKeyAndGradeRandomnessShareMessage GroupKeyMessage
    )> DeserializeReviewsMessageAsync(LogEntry reviewsEntry, List<LogEntry> groupKeyEntries, byte[] sharedKey)
    {
        foreach (var groupKeyEntry in groupKeyEntries)
        {
            try
            {
                var groupKeyMessage = await GroupKeyAndGradeRandomnessShareMessage.DeserializeAsync(
                    groupKeyEntry.Data,
                    sharedKey
                );
                var matchingMessage = await FindMatchingMessageAsync(groupKeyMessage.Paper, sharedKey);
                var reviewsMessage = await ReviewsShareMessage.DeserializeAsync(
                    reviewsEntry.Data,
                    groupKeyMessage.GroupKey,
                    matchingMessage.ReviewerPublicKeys
                );
                return (reviewsMessage, matchingMessage, groupKeyMessage);
            }
            catch (CryptographicException) { }
        }

        throw new InvalidOperationException(
            $"A matching {ProtocolStep.GroupKeyAndGradeRandomnessShare} entry for the "
                + $"{ProtocolStep.ReviewsShare} entry was not found."
        );
    }

    private async Task<PaperReviewersMatchingMessage> FindMatchingMessageAsync(byte[] paperBytes, byte[] sharedKey)
    {
        var paperHash = await Task.Run(() => SHA256.HashData(paperBytes));

        await using var logDbContext = _logDbContextFactory();
        var matchingEntries = await logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.PaperReviewersMatching)
            .ToListAsync();

        foreach (var matchingEntry in matchingEntries)
        {
            var matchingMessage = await PaperReviewersMatchingMessage.DeserializeAsync(matchingEntry.Data);
            var shareEntries = logDbContext
                .Entries.Where(entry => entry.Step == ProtocolStep.PaperAndReviewRandomnessShare)
                .AsAsyncEnumerable();

            await foreach (var shareEntry in shareEntries)
            {
                var shareMessage = await PaperAndReviewRandomnessShareMessage.DeserializeAsync(
                    shareEntry.Data,
                    sharedKey
                );
                var reviewRandomness = new BigInteger(shareMessage.ReviewRandomness);
                var reviewCommitment = Commitment.Create(paperBytes, reviewRandomness);

                if (!reviewCommitment.ToBytes().SequenceEqual(matchingMessage.ReviewCommitment))
                {
                    continue;
                }

                return matchingMessage;
            }
        }

        throw new InvalidOperationException(
            $"A matching {ProtocolStep.PaperReviewersMatching} entry for the paper was not found."
        );
    }

    public async Task DownloadPaperAsync(byte[] paperHash, string paperFilePath)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var logDbContext = _logDbContextFactory();
        var shareEntries = logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.PaperShare)
            .AsAsyncEnumerable();

        await foreach (var shareEntry in shareEntries)
        {
            PaperShareMessage shareMessage;

            try
            {
                shareMessage = await PaperShareMessage.DeserializeAsync(shareEntry.Data, sharedKey);
            }
            catch (CryptographicException)
            {
                continue;
            }

            var sharePaperHash = await Task.Run(() => SHA256.HashData(shareMessage.Paper));

            if (!sharePaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            await File.WriteAllBytesAsync(paperFilePath, shareMessage.Paper);
            return;
        }

        throw new InvalidOperationException(
            $"A matching {ProtocolStep.PaperShare} entry for the paper hash was not found."
        );
    }
}
