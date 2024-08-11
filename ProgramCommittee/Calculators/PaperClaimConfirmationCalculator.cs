using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared;
using Apachi.Shared.Data;
using Apachi.Shared.Factories;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee.Calculators;

public class PaperClaimConfirmationCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public PaperClaimConfirmationCalculator(
        AppDbContext appDbContext,
        LogDbContext logDbContext,
        MessageFactory messageFactory
    )
    {
        _appDbContext = appDbContext;
        _logDbContext = logDbContext;
        _messageFactory = messageFactory;
    }

    public async Task CalculateAsync(CancellationToken cancellationToken)
    {
        var publicKeyMessages = _messageFactory.GetCommitmentsAndPublicKeyMessagesAsync();

        await foreach (var publicKeyMessage in publicKeyMessages)
        {
            var claimMessage = await _messageFactory.GetClaimMessageBySubmissionPublicKeyAsync(
                publicKeyMessage.SubmissionPublicKey
            );

            if (claimMessage == null)
            {
                continue;
            }

            var paperHash = SHA256.HashData(claimMessage.Paper);
            var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperClaimConfirmation && @event.Identifier == paperHash
            );

            if (hasExisting)
            {
                continue;
            }

            var confirmationMessage = new PaperClaimConfirmationMessage
            {
                Paper = claimMessage.Paper,
                Identity = claimMessage.Identity,
                IdentityRandomness = claimMessage.IdentityRandomness
            };
            var confirmationEntry = new LogEntry
            {
                Step = ProtocolStep.PaperClaimConfirmation,
                Data = await confirmationMessage.SerializeAsync()
            };
            _logDbContext.Entries.Add(confirmationEntry);

            var logEvent = new LogEvent { Step = confirmationEntry.Step, Identifier = paperHash };
            _appDbContext.LogEvents.Add(logEvent);
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }
}
