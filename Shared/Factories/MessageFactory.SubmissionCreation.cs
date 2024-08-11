using System.Runtime.Serialization;
using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Apachi.Shared.Messages;
using Org.BouncyCastle.Math;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<SubmissionCreationMessage?> GetCreationMessageBySubmissionCommitmentAsync(
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

        return null;
    }

    public async Task<SubmissionCreationMessage?> GetCreationMessageByReviewCommitmentAsync(byte[] reviewCommitment)
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

        return null;
    }

    public async IAsyncEnumerable<SubmissionCreationMessage> GetCreationMessagesAsync()
    {
        var publicKeyMessages = await GetCommitmentsAndPublicKeyMessagesAsync().ToListAsync();
        var creationEntries = EnumerateEntriesAsync(ProtocolStep.SubmissionCreation);

        await foreach (var creationEntry in creationEntries)
        {
            foreach (var publicKeyMessage in publicKeyMessages)
            {
                SubmissionCreationMessage creationMessage;

                try
                {
                    creationMessage = await SubmissionCreationMessage.DeserializeAsync(
                        creationEntry.Data,
                        publicKeyMessage.SubmissionPublicKey
                    );
                }
                catch (Exception exception) when (exception is CryptographicException or SerializationException)
                {
                    continue;
                }

                yield return creationMessage;
            }
        }
    }

    public async Task<SubmissionCreationMessage?> GetCreationMessageBySubmissionKeyAsync(
        byte[] submissionKey,
        byte[] submissionPublicKey
    )
    {
        var creationEntries = EnumerateEntriesAsync(ProtocolStep.SubmissionCreation);

        await foreach (var creationEntry in creationEntries)
        {
            try
            {
                var creationMessage = await SubmissionCreationMessage.DeserializeAsSubmitterAsync(
                    creationEntry.Data,
                    submissionKey,
                    submissionPublicKey
                );
                return creationMessage;
            }
            catch (Exception exception) when (exception is CryptographicException or SerializationException)
            {
                continue;
            }
        }

        return null;
    }
}
