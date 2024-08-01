using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;

namespace Apachi.ProgramCommittee.Calculators;

public class PaperAndReviewRandomnessShareCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;

    public PaperAndReviewRandomnessShareCalculator(AppDbContext appDbContext, LogDbContext logDbContext)
    {
        _appDbContext = appDbContext;
        _logDbContext = logDbContext;
    }

    public async Task CalculateAsync(CancellationToken cancellationToken)
    {
        var publicKeyEntries = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.SubmissionCommitmentsAndPublicKey)
            .ToListAsync();
        var creationEntryIds = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.SubmissionCreation)
            .Select(entry => entry.Id)
            .ToListAsync();

        foreach (var creationEntryId in creationEntryIds)
        {
            var creationEntry = await _logDbContext.Entries.SingleAsync(entry => entry.Id == creationEntryId);
            var creationMessage = await DeserializeCreationMessageAsync(creationEntry, publicKeyEntries);
            var paperHash = SHA256.HashData(creationMessage.Paper);
            var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperAndReviewRandomnessShare && @event.Identifier == paperHash
            );

            if (hasExisting)
            {
                continue;
            }

            var hasMatching = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperReviewersMatching && @event.Identifier == paperHash
            );

            if (!hasMatching)
            {
                continue;
            }

            var shareMessage = new PaperAndReviewRandomnessShareMessage
            {
                Paper = creationMessage.Paper,
                ReviewRandomness = creationMessage.ReviewRandomness
            };
            var matchingMessage = await FindMatchingMessageAsync(shareMessage);

            var pcPrivateKey = GetPCPrivateKey();
            var reviewers = await _logDbContext
                .Reviewers.Where(reviewer => matchingMessage.ReviewerPublicKeys.Any(key => key == reviewer.PublicKey))
                .ToListAsync();

            foreach (var reviewer in reviewers)
            {
                var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
                var shareEntry = new LogEntry
                {
                    Step = ProtocolStep.PaperAndReviewRandomnessShare,
                    Data = await shareMessage.SerializeAsync(sharedKey)
                };
                _logDbContext.Entries.Add(shareEntry);

                var logEvent = new LogEvent { Step = shareEntry.Step, Identifier = paperHash };
                _appDbContext.LogEvents.Add(logEvent);
            }
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }

    private async Task<SubmissionCreationMessage> DeserializeCreationMessageAsync(
        LogEntry creationEntry,
        List<LogEntry> publicKeyEntries
    )
    {
        foreach (var publicKeyEntry in publicKeyEntries)
        {
            var publicKeyMessage = await SubmissionCommitmentsAndPublicKeyMessage.DeserializeAsync(publicKeyEntry.Data);

            try
            {
                var creationMessage = await SubmissionCreationMessage.DeserializeAsync(
                    creationEntry.Data,
                    publicKeyMessage.SubmissionPublicKey
                );
                return creationMessage;
            }
            catch (CryptographicException) { }
        }

        throw new InvalidOperationException(
            $"A matching {ProtocolStep.SubmissionCommitmentsAndPublicKey} entry "
                + $"for the {ProtocolStep.SubmissionCreation} entry was not found."
        );
    }

    private async Task<PaperReviewersMatchingMessage> FindMatchingMessageAsync(
        PaperAndReviewRandomnessShareMessage shareMessage
    )
    {
        var reviewRandomness = new BigInteger(shareMessage.ReviewRandomness);
        var reviewCommitment = Commitment.Create(shareMessage.Paper, reviewRandomness);
        var reviewCommitmentBytes = reviewCommitment.ToBytes();

        var matchingEntries = _logDbContext
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
                + $"{ProtocolStep.PaperAndReviewRandomnessShare} entry was not found."
        );
    }
}
