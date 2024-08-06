using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<SubmissionCreationMessage> GetCreationMessageBySubmissionCommitmentAsync(
        byte[] submissionCommitment
    )
    {
        var creationMessages = GetCreationMessagesAsync();

        await foreach (var creationMessage in creationMessages)
        {
            var submissionRandomness = new BigInteger(creationMessage.SubmissionRandomness);
            var messageSubmissionCommitment = Commitment.Create(creationMessage.Paper, submissionRandomness);

            if (!messageSubmissionCommitment.ToBytes().SequenceEqual(submissionCommitment))
            {
                continue;
            }

            return creationMessage;
        }

        throw new MessageCreationException(ProtocolStep.SubmissionCreation);
    }

    public async Task<SubmissionCreationMessage> GetCreationMessageByReviewCommitmentAsync(byte[] reviewCommitment)
    {
        var creationMessages = GetCreationMessagesAsync();

        await foreach (var creationMessage in creationMessages)
        {
            var reviewRandomness = new BigInteger(creationMessage.ReviewRandomness);
            var messageReviewCommitment = Commitment.Create(creationMessage.Paper, reviewRandomness);

            if (!messageReviewCommitment.ToBytes().SequenceEqual(reviewCommitment))
            {
                continue;
            }

            return creationMessage;
        }

        throw new MessageCreationException(ProtocolStep.SubmissionCreation);
    }

    public async IAsyncEnumerable<SubmissionCreationMessage> GetCreationMessagesAsync()
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

            foreach (var publicKeyEntry in publicKeyEntries)
            {
                var publicKeyMessage = await SubmissionCommitmentsAndPublicKeyMessage.DeserializeAsync(
                    publicKeyEntry.Data
                );
                SubmissionCreationMessage creationMessage;

                try
                {
                    creationMessage = await SubmissionCreationMessage.DeserializeAsync(
                        creationEntry.Data,
                        publicKeyMessage.SubmissionPublicKey
                    );
                }
                catch (CryptographicException)
                {
                    continue;
                }

                yield return creationMessage;
            }
        }
    }
}
