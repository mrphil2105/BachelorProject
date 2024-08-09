using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Apachi.Shared.Messages;
using Org.BouncyCastle.Math;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<PaperReviewersMatchingMessage> GetMatchingMessageByPaperAsync(byte[] paper, byte[] sharedKey)
    {
        var paperHash = await Task.Run(() => SHA256.HashData(paper));
        var paperMessage = await GetPaperAndRandomnessMessageByPaperHashAsync(paperHash, sharedKey);
        var reviewRandomness = new BigInteger(paperMessage.ReviewRandomness);
        var reviewCommitment = Commitment.Create(paper, reviewRandomness);

        var matchingMessage = await GetMatchingMessageByCommitmentAsync(reviewCommitment.ToBytes());
        return matchingMessage;
    }

    public async Task<PaperReviewersMatchingMessage> GetMatchingMessageByCommitmentAsync(byte[] reviewCommitment)
    {
        var matchingMessages = GetMatchingMessagesAsync();

        await foreach (var matchingMessage in matchingMessages)
        {
            if (matchingMessage.ReviewCommitment.SequenceEqual(reviewCommitment))
            {
                return matchingMessage;
            }
        }

        throw new MessageCreationException(ProtocolStep.PaperReviewersMatching);
    }

    public async IAsyncEnumerable<PaperReviewersMatchingMessage> GetMatchingMessagesAsync()
    {
        var matchingEntries = await GetEntriesAsync(ProtocolStep.PaperReviewersMatching);

        foreach (var matchingEntry in matchingEntries)
        {
            var matchingMessage = await PaperReviewersMatchingMessage.DeserializeAsync(matchingEntry.Data);
            yield return matchingMessage;
        }
    }
}
