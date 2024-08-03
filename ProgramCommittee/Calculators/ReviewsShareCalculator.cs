using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;

namespace Apachi.ProgramCommittee.Calculators;

public class ReviewsShareCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;

    public ReviewsShareCalculator(AppDbContext appDbContext, LogDbContext logDbContext)
    {
        _appDbContext = appDbContext;
        _logDbContext = logDbContext;
    }

    public async Task CalculateAsync(CancellationToken cancellationToken)
    {
        var creationEntryIds = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.SubmissionCreation)
            .Select(entry => entry.Id)
            .ToListAsync();

        foreach (var creationEntryId in creationEntryIds)
        {
            var creationEntry = await _logDbContext.Entries.SingleAsync(entry => entry.Id == creationEntryId);
            var creationMessage = await DeserializeCreationMessageAsync(creationEntry);

            var reviewRandomness = new BigInteger(creationMessage.ReviewRandomness);
            var reviewCommitment = Commitment.Create(creationMessage.Paper, reviewRandomness);
            var reviewCommitmentBytes = reviewCommitment.ToBytes();

            var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.ReviewsShare && @event.Identifier == reviewCommitmentBytes
            );

            if (hasExisting)
            {
                continue;
            }

            var shareCount = await _appDbContext.LogEvents.CountAsync(@event =>
                @event.Step == ProtocolStep.GroupKeyAndGradeRandomnessShare
                && @event.Identifier == reviewCommitmentBytes
            );
            var matchingMessage = await FindMatchingMessageAsync(creationMessage);

            if (shareCount != matchingMessage.ReviewerPublicKeys.Count)
            {
                // The group key has not been shared with all reviewers yet.
                continue;
            }

            var pcPrivateKey = GetPCPrivateKey();
            var paperHash = SHA256.HashData(creationMessage.Paper);

            var reviewers = await _logDbContext
                .Reviewers.Where(reviewer => matchingMessage.ReviewerPublicKeys.Any(key => key == reviewer.PublicKey))
                .ToListAsync();

            var reviews = new List<byte[]>();
            var reviewSignatures = new List<byte[]>();
            byte[]? groupKey = null;

            foreach (var reviewer in reviewers)
            {
                var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
                var reviewMessage = await FindReviewMessageAsync(reviewer, paperHash);
                reviews.Add(reviewMessage.Review);
                reviewSignatures.Add(reviewMessage.ReviewSignature);

                groupKey ??= await FindGroupKeyAsync(reviewer, paperHash);
            }

            var shareMessage = new ReviewsShareMessage { Reviews = reviews };
            var shareEntry = new LogEntry
            {
                Step = ProtocolStep.ReviewsShare,
                Data = await shareMessage.SerializeAsync(reviewSignatures, groupKey!)
            };
            _logDbContext.Entries.Add(shareEntry);

            var logEvent = new LogEvent { Step = ProtocolStep.ReviewsShare, Identifier = reviewCommitmentBytes };
            _appDbContext.LogEvents.Add(logEvent);
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }

    private async Task<SubmissionCreationMessage> DeserializeCreationMessageAsync(LogEntry creationEntry)
    {
        var publicKeyEntries = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.SubmissionCommitmentsAndPublicKey)
            .ToListAsync();

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
        SubmissionCreationMessage creationMessage
    )
    {
        var reviewRandomness = new BigInteger(creationMessage.ReviewRandomness);
        var reviewCommitment = Commitment.Create(creationMessage.Paper, reviewRandomness);
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

    private async Task<ReviewMessage> FindReviewMessageAsync(Reviewer reviewer, byte[] paperHash)
    {
        var pcPrivateKey = GetPCPrivateKey();
        var reviewEntries = _logDbContext.Entries.Where(entry => entry.Step == ProtocolStep.Review).AsAsyncEnumerable();

        await foreach (var reviewEntry in reviewEntries)
        {
            var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
            ReviewMessage reviewMessage;

            try
            {
                reviewMessage = await ReviewMessage.DeserializeAsync(reviewEntry.Data, sharedKey, reviewer.PublicKey);
            }
            catch (CryptographicException)
            {
                continue;
            }

            var reviewPaperHash = SHA256.HashData(reviewMessage.Paper);

            if (!reviewPaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            return reviewMessage;
        }

        throw new InvalidOperationException($"A matching {ProtocolStep.Review} entry for the reviewer was not found.");
    }

    private async Task<byte[]> FindGroupKeyAsync(Reviewer reviewer, byte[] paperHash)
    {
        var pcPrivateKey = GetPCPrivateKey();
        var shareEntries = _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.GroupKeyAndGradeRandomnessShare)
            .AsAsyncEnumerable();

        await foreach (var shareEntry in shareEntries)
        {
            var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
            GroupKeyAndGradeRandomnessShareMessage shareMessage;

            try
            {
                shareMessage = await GroupKeyAndGradeRandomnessShareMessage.DeserializeAsync(
                    shareEntry.Data,
                    sharedKey
                );
            }
            catch (CryptographicException)
            {
                continue;
            }

            var sharePaperHash = SHA256.HashData(shareMessage.Paper);

            if (!sharePaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            return shareMessage.GroupKey;
        }

        throw new InvalidOperationException(
            $"A matching {ProtocolStep.GroupKeyAndGradeRandomnessShare} entry for the reviewer was not found."
        );
    }
}
