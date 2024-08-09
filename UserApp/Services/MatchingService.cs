using System.Security.Cryptography;
using Apachi.Shared;
using Apachi.Shared.Data;
using Apachi.Shared.Factories;
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
    private readonly Func<MessageFactory> _messageFactoryFactory;

    public MatchingService(
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

    public async Task<List<MatchableSubmissionModel>> GetMatchableSubmissionsAsync()
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var messageFactory = _messageFactoryFactory();
        var paperMessages = messageFactory.GetPaperMessagesAsync(sharedKey);
        var models = new List<MatchableSubmissionModel>();

        await foreach (var paperMessage in paperMessages)
        {
            var paperHash = await Task.Run(() => SHA256.HashData(paperMessage.Paper));
            var hasBid = await appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.Bid
                && @event.Identifier == paperHash
                && @event.ReviewerId == _sessionService.UserId!.Value
            );

            if (hasBid)
            {
                continue;
            }

            var model = new MatchableSubmissionModel(paperHash);
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

    public async Task SendBidAsync(byte[] paperHash, bool wantsToReview)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedPrivateKey);
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var messageFactory = _messageFactoryFactory();
        var paperMessage = await messageFactory.GetPaperMessageByPaperHashAsync(paperHash, sharedKey);

        var bidMessage = new BidMessage
        {
            Paper = paperMessage.Paper,
            Bid = new byte[] { (byte)(wantsToReview ? 1 : 0) }
        };
        var bidEntry = new LogEntry
        {
            Step = ProtocolStep.Bid,
            Data = await bidMessage.SerializeAsync(privateKey, sharedKey)
        };

        await using var logDbContext = _logDbContextFactory();
        logDbContext.Entries.Add(bidEntry);

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
