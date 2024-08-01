using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;

namespace Apachi.ProgramCommittee.Calculators;

public class PaperReviewersMatchingCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;

    public PaperReviewersMatchingCalculator(AppDbContext appDbContext, LogDbContext logDbContext)
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
                @event.Step == ProtocolStep.PaperReviewersMatching && @event.Identifier == paperHash
            );

            if (hasExisting)
            {
                continue;
            }

            var reviewerCount = await _appDbContext.LogEvents.CountAsync(@event =>
                @event.Step == ProtocolStep.PaperShare && @event.Identifier == paperHash
            );

            if (reviewerCount == 0)
            {
                // The paper has not been shared with any reviewers yet.
                continue;
            }

            var bidEntries = _logDbContext.Entries.Where(entry => entry.Step == ProtocolStep.Bid);
            var reviewers = await _logDbContext.Reviewers.ToListAsync();

            var bidCount = 0;
            var reviewerPublicKeys = new List<byte[]>();

            foreach (var bidEntry in bidEntries)
            {
                var (bidMessage, reviewer) = await DeserializeBidMessageAsync(bidEntry, reviewers);
                bidCount++;

                if (bidMessage.Bid[0] == 0)
                {
                    // Reviewer has chosen to abstain from reviewing the paper.
                    continue;
                }

                reviewerPublicKeys.Add(reviewer.PublicKey);
            }

            if (bidCount != reviewerCount)
            {
                // Not all reviewers have sent their bid yet.
                continue;
            }

            var reviewRandomness = new BigInteger(creationMessage.ReviewRandomness);
            var reviewCommitment = Commitment.Create(creationMessage.Paper, reviewRandomness);

            var matchingMessage = new PaperReviewersMatchingMessage
            {
                ReviewCommitment = reviewCommitment.ToBytes(),
                ReviewerPublicKeys = reviewerPublicKeys,
                ReviewNonce = GenerateBigInteger().ToByteArray(),
                // TODO: Add proof that the paper submission commitment and paper review commitment hide the same paper.
                EqualityProof = Array.Empty<byte>()
            };
            var matchingEntry = new LogEntry
            {
                Step = ProtocolStep.PaperReviewersMatching,
                Data = await matchingMessage.SerializeAsync()
            };
            _logDbContext.Entries.Add(matchingEntry);

            var logEvent = new LogEvent { Step = matchingEntry.Step, Identifier = paperHash };
            _appDbContext.LogEvents.Add(logEvent);
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

    private async Task<(BidMessage BidMessage, Reviewer Reviewer)> DeserializeBidMessageAsync(
        LogEntry bidEntry,
        List<Reviewer> reviewers
    )
    {
        var pcPrivateKey = GetPCPrivateKey();

        foreach (var reviewer in reviewers)
        {
            try
            {
                var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
                var bidMessage = await BidMessage.DeserializeAsync(bidEntry.Data, sharedKey, reviewer.PublicKey);
                return (bidMessage, reviewer);
            }
            catch (CryptographicException) { }
        }

        throw new InvalidOperationException($"A matching reviewer for the {ProtocolStep.Bid} entry was not found.");
    }
}
