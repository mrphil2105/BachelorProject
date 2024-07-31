using System.Security.Cryptography;
using Apachi.Shared;
using Apachi.Shared.Data;
using Apachi.Shared.Messages;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;

namespace Apachi.UserApp.Services;

public class MatchingService : IMatchingService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _appDbContextFactory;
    private readonly Func<LogDbContext> _logDbContextFactory;

    public MatchingService(
        ISessionService sessionService,
        Func<AppDbContext> appDbContextFactory,
        Func<LogDbContext> logDbContextFactory
    )
    {
        _sessionService = sessionService;
        _appDbContextFactory = appDbContextFactory;
        _logDbContextFactory = logDbContextFactory;
    }

    public async Task<List<MatchableSubmissionModel>> GetMatchableSubmissionsAsync()
    {
        var models = new List<MatchableSubmissionModel>();

        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var logDbContext = _logDbContextFactory();
        var shareEntries = logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.PaperReviewerShare)
            .AsAsyncEnumerable();

        await foreach (var shareEntry in shareEntries)
        {
            PaperReviewerShareMessage shareMessage;

            try
            {
                shareMessage = await PaperReviewerShareMessage.DeserializeAsync(shareEntry.Data, sharedKey);
            }
            catch (CryptographicException)
            {
                continue;
            }

            var paperHash = await Task.Run(() => SHA256.HashData(shareMessage.Paper));
            var hasBid = await appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.Bid
                && @event.Identifier == paperHash
                && @event.ReviewerId == _sessionService.UserId!.Value
            );

            if (hasBid)
            {
                continue;
            }

            var model = new MatchableSubmissionModel(shareEntry.Id, shareEntry.CreatedDate);
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
        var shareMessage = await PaperReviewerShareMessage.DeserializeAsync(shareEntry.Data, sharedKey);

        await File.WriteAllBytesAsync(paperFilePath, shareMessage.Paper);
    }

    public async Task SendBidAsync(Guid logEntryId, bool wantsToReview)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedPrivateKey);
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var logDbContext = _logDbContextFactory();
        var shareEntry = await logDbContext.Entries.SingleAsync(entry => entry.Id == logEntryId);
        var shareMessage = await PaperReviewerShareMessage.DeserializeAsync(shareEntry.Data, sharedKey);

        var bidMessage = new BidMessage
        {
            Paper = shareMessage.Paper,
            Bid = new byte[] { (byte)(wantsToReview ? 1 : 0) }
        };
        var bidEntry = new LogEntry
        {
            Step = ProtocolStep.Bid,
            Data = await bidMessage.SerializeAsync(privateKey, sharedKey)
        };
        logDbContext.Entries.Add(bidEntry);

        var paperHash = await Task.Run(() => SHA256.HashData(shareMessage.Paper));
        var logEvent = new LogEvent
        {
            Step = bidEntry.Step,
            Identifier = paperHash,
            ReviewerId = _sessionService.UserId!.Value
        };
        appDbContext.LogEvents.Add(logEvent);

        await logDbContext.SaveChangesAsync();
        await appDbContext.SaveChangesAsync();
    }
}