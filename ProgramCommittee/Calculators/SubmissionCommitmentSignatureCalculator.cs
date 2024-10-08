using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared;
using Apachi.Shared.Data;
using Apachi.Shared.Factories;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee.Calculators;

public class SubmissionCommitmentSignatureCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public SubmissionCommitmentSignatureCalculator(
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
        var hasAcceptedGrades = await _appDbContext.LogEvents.AnyAsync(@event =>
            @event.Step == ProtocolStep.AcceptedGrades
        );

        if (hasAcceptedGrades)
        {
            // The PC has published all accepted grades, so new submissions cannot be confirmed.
            return;
        }

        var commitmentMessages = _messageFactory.GetCommitmentsAndPublicKeyMessagesAsync();

        await foreach (var commitmentMessage in commitmentMessages)
        {
            var creationMessage = await _messageFactory.GetCreationMessageBySubmissionCommitmentAsync(
                commitmentMessage.SubmissionCommitment
            );

            var paperHash = SHA256.HashData(creationMessage!.Paper);
            var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.SubmissionCommitmentSignature && @event.Identifier == paperHash
            );

            if (hasExisting)
            {
                continue;
            }

            var signatureMessage = new SubmissionCommitmentSignatureMessage
            {
                SubmissionCommitment = commitmentMessage.SubmissionCommitment
            };
            var signatureEntry = new LogEntry
            {
                Step = ProtocolStep.SubmissionCommitmentSignature,
                Data = await signatureMessage.SerializeAsync()
            };
            _logDbContext.Entries.Add(signatureEntry);

            var logEvent = new LogEvent { Step = signatureEntry.Step, Identifier = paperHash };
            _appDbContext.LogEvents.Add(logEvent);
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }
}
