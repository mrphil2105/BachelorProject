using System.Security.Cryptography;
using Apachi.Shared;
using Apachi.Shared.Data;
using Apachi.Shared.Factories;
using Apachi.Shared.Messages;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;

namespace Apachi.UserApp.Services;

public class ClaimService : IClaimService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _appDbContextFactory;
    private readonly Func<LogDbContext> _logDbContextFactory;
    private readonly Func<MessageFactory> _messageFactoryFactory;

    public ClaimService(
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

    public async Task ClaimAcceptedPapers()
    {
        await using var appDbContext = _appDbContextFactory();
        var submissions = await appDbContext
            .Submissions.Where(submission => submission.SubmitterId == _sessionService.UserId!.Value)
            .ToListAsync();

        await using var logDbContext = _logDbContextFactory();
        await using var messageFactory = _messageFactoryFactory();

        foreach (var submission in submissions)
        {
            var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(submission.EncryptedPrivateKey);
            var submissionKey = await _sessionService.SymmetricDecryptAndVerifyAsync(submission.EncryptedSubmissionKey);
            var publicKey = GetPublicKeyFromPrivateKey(privateKey);

            var creationMessage = await messageFactory.GetCreationMessageBySubmissionKeyAsync(submissionKey, publicKey);
            var paperHash = await Task.Run(() => SHA256.HashData(creationMessage!.Paper));
            var hasClaimed = await appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperClaim
                && @event.Identifier == paperHash
                && @event.UserId == _sessionService.UserId!.Value
            );

            if (hasClaimed)
            {
                continue;
            }

            var revealMessage = await messageFactory.GetRevealMessageByPaperHashAsync(paperHash);

            if (revealMessage == null)
            {
                continue;
            }

            var idBytes = _sessionService.UserId!.Value.ToByteArray(true);
            var identityRandomness = await _sessionService.SymmetricDecryptAndVerifyAsync(
                submission.EncryptedIdentityRandomness
            );

            var claimMessage = new PaperClaimMessage
            {
                Paper = creationMessage!.Paper,
                Identity = idBytes,
                IdentityRandomness = identityRandomness
            };
            var claimEntry = new LogEntry
            {
                Step = ProtocolStep.PaperClaim,
                Data = await claimMessage.SerializeAsync(privateKey)
            };
            logDbContext.Entries.Add(claimEntry);

            var logEvent = new LogEvent
            {
                Step = claimEntry.Step,
                Identifier = paperHash,
                UserId = _sessionService.UserId!.Value
            };
            appDbContext.LogEvents.Add(logEvent);
        }

        await logDbContext.SaveChangesAsync();
        await appDbContext.SaveChangesAsync();
    }
}
