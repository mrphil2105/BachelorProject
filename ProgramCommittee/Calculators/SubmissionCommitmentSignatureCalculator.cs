using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;

namespace Apachi.ProgramCommittee.Calculators;

public class SubmissionCommitmentSignatureCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;

    public SubmissionCommitmentSignatureCalculator(AppDbContext appDbContext, LogDbContext logDbContext)
    {
        _appDbContext = appDbContext;
        _logDbContext = logDbContext;
    }

    public async Task CalculateAsync(CancellationToken cancellationToken)
    {
        var commitmentEntryIds = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.SubmissionCommitmentsAndPublicKey)
            .Select(entry => entry.Id)
            .ToListAsync();

        foreach (var commitmentEntryId in commitmentEntryIds)
        {
            var commitmentEntry = await _logDbContext.Entries.SingleAsync(entry => entry.Id == commitmentEntryId);
            var commitmentMessage = await SubmissionCommitmentsAndPublicKeyMessage.DeserializeAsync(
                commitmentEntry.Data
            );
            var creationMessage = await FindCreationMessageAsync(commitmentMessage);
            var paperHash = SHA256.HashData(creationMessage.Paper);
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

    private async Task<SubmissionCreationMessage> FindCreationMessageAsync(
        SubmissionCommitmentsAndPublicKeyMessage commitmentMessage
    )
    {
        var publicKeyEntries = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.SubmissionCommitmentsAndPublicKey)
            .ToListAsync();
        var creationEntries = _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.SubmissionCreation)
            .AsAsyncEnumerable();

        await foreach (var creationEntry in creationEntries)
        {
            foreach (var publicKeyEntry in publicKeyEntries)
            {
                var publicKeyMessage = await SubmissionCommitmentsAndPublicKeyMessage.DeserializeAsync(
                    publicKeyEntry.Data
                );

                try
                {
                    var creationMessage = await SubmissionCreationMessage.DeserializeAsync(
                        creationEntry.Data,
                        publicKeyMessage.SubmissionPublicKey
                    );

                    var submissionRandomness = new BigInteger(creationMessage.SubmissionRandomness);
                    var submissionCommitment = Commitment.Create(creationMessage.Paper, submissionRandomness);

                    if (submissionCommitment.ToBytes().SequenceEqual(commitmentMessage.SubmissionCommitment))
                    {
                        return creationMessage;
                    }
                }
                catch (CryptographicException) { }
            }
        }

        throw new InvalidOperationException(
            $"A matching {ProtocolStep.SubmissionCreation} entry for the "
                + $"{ProtocolStep.SubmissionCommitmentsAndPublicKey} entry was not found."
        );
    }
}
