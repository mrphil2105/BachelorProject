using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;

namespace Apachi.ProgramCommittee.Calculators;

public class GroupKeyAndGradeRandomnessShareCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;

    public GroupKeyAndGradeRandomnessShareCalculator(AppDbContext appDbContext, LogDbContext logDbContext)
    {
        _appDbContext = appDbContext;
        _logDbContext = logDbContext;
    }

    public async Task CalculateAsync(CancellationToken cancellationToken)
    {
        var matchingEntries = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.PaperReviewersMatching)
            .ToListAsync();
        var signaturesEntries = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.ReviewCommitmentAndNonceSignature)
            .ToListAsync();

        foreach (var matchingEntry in matchingEntries)
        {
            var matchingMessage = await PaperReviewersMatchingMessage.DeserializeAsync(matchingEntry.Data);
            var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.GroupKeyAndGradeRandomnessShare
                && @event.Identifier == matchingMessage.ReviewCommitment
            );

            if (hasExisting)
            {
                continue;
            }

            var reviewCount = 0;

            foreach (var signatureEntry in signaturesEntries)
            {
                var signatureMessage = await TryDeserializeSignatureMessageAsync(
                    signatureEntry,
                    matchingMessage.ReviewerPublicKeys
                );

                if (signatureMessage == null)
                {
                    // The signature message is from a reviewer not involved with the current paper, since no associated
                    // private key for a public key in 'ReviewerPublicKeys' has signed it.
                    continue;
                }

                // The signature message can still be for another paper, in case the reviewer for the other paper is
                // also reviewing current paper. So we need to check the review commitment to be sure.
                if (!signatureMessage.ReviewCommitment.SequenceEqual(matchingMessage.ReviewCommitment))
                {
                    continue;
                }

                reviewCount++;
            }

            if (reviewCount != matchingMessage.ReviewerPublicKeys.Count)
            {
                // Not all reviewers have signed the review commitment and nonce.
                continue;
            }

            var creationMessage = await FindCreationMessageAsync(matchingMessage);
            var shareMessage = new GroupKeyAndGradeRandomnessShareMessage
            {
                Paper = creationMessage.Paper,
                GroupKey = RandomNumberGenerator.GetBytes(32),
                GradeRandomness = GenerateBigInteger().ToByteArray()
            };

            var pcPrivateKey = GetPCPrivateKey();
            var reviewers = await _logDbContext
                .Reviewers.Where(reviewer => matchingMessage.ReviewerPublicKeys.Any(key => key == reviewer.PublicKey))
                .ToListAsync();

            foreach (var reviewer in reviewers)
            {
                var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
                var shareEntry = new LogEntry
                {
                    Step = ProtocolStep.GroupKeyAndGradeRandomnessShare,
                    Data = await shareMessage.SerializeAsync(sharedKey)
                };
                _logDbContext.Entries.Add(shareEntry);

                var logEvent = new LogEvent { Step = shareEntry.Step, Identifier = matchingMessage.ReviewCommitment };
                _appDbContext.LogEvents.Add(logEvent);
            }
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }

    private async Task<ReviewCommitmentAndNonceSignatureMessage?> TryDeserializeSignatureMessageAsync(
        LogEntry signatureEntry,
        List<byte[]> reviewerPublicKeys
    )
    {
        foreach (var reviewerPublicKey in reviewerPublicKeys)
        {
            try
            {
                var signatureMessage = await ReviewCommitmentAndNonceSignatureMessage.DeserializeAsync(
                    signatureEntry.Data,
                    reviewerPublicKey
                );
                return signatureMessage;
            }
            catch (CryptographicException) { }
        }

        return null;
    }

    private async Task<SubmissionCreationMessage> FindCreationMessageAsync(
        PaperReviewersMatchingMessage matchingMessage
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
                    var reviewRandomness = new BigInteger(creationMessage.ReviewRandomness);
                    var reviewCommitment = Commitment.Create(creationMessage.Paper, reviewRandomness);

                    if (reviewCommitment.ToBytes().SequenceEqual(matchingMessage.ReviewCommitment))
                    {
                        return creationMessage;
                    }
                }
                catch (CryptographicException) { }
            }
        }

        throw new InvalidOperationException(
            $"A matching {ProtocolStep.SubmissionCreation} entry for the "
                + $"{ProtocolStep.PaperReviewersMatching} entry was not found."
        );
    }
}
