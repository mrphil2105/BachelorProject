using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared;
using Apachi.Shared.Data;
using Apachi.Shared.Factories;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee.Calculators;

public class PaperShareCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public PaperShareCalculator(AppDbContext appDbContext, LogDbContext logDbContext, MessageFactory messageFactory)
    {
        _appDbContext = appDbContext;
        _logDbContext = logDbContext;
        _messageFactory = messageFactory;
    }

    public async Task CalculateAsync(CancellationToken cancellationToken)
    {
        var creationMessages = _messageFactory.GetCreationMessagesAsync();

        await foreach (var creationMessage in creationMessages)
        {
            var paperHash = SHA256.HashData(creationMessage.Paper);
            var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperShare && @event.Identifier == paperHash
            );

            if (hasExisting)
            {
                continue;
            }

            var hasConfirmed = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.SubmissionCommitmentSignature && @event.Identifier == paperHash
            );

            if (!hasConfirmed)
            {
                continue;
            }

            var reviewers = await _logDbContext.Reviewers.ToListAsync();
            var paperMessage = new PaperShareMessage { Paper = creationMessage.Paper };

            foreach (var reviewer in reviewers)
            {
                var sharedKey = await reviewer.DecryptSharedKeyAsync();
                var paperEntry = new LogEntry
                {
                    Step = ProtocolStep.PaperShare,
                    Data = await paperMessage.SerializeAsync(sharedKey)
                };
                _logDbContext.Entries.Add(paperEntry);

                var logEvent = new LogEvent { Step = paperEntry.Step, Identifier = paperHash };
                _appDbContext.LogEvents.Add(logEvent);
            }
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }
}
