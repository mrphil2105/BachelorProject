using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;
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

        var matchingMessage = await GetMatchingMessageByCommitmentAsync(reviewCommitment);
        return matchingMessage;
    }

    public async Task<PaperReviewersMatchingMessage> GetMatchingMessageByCommitmentAsync(Commitment reviewCommitment)
    {
        var reviewCommitmentBytes = reviewCommitment.ToBytes();
        var matchingEntries = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.PaperReviewersMatching)
            .ToListAsync();

        foreach (var matchingEntry in matchingEntries)
        {
            var matchingMessage = await PaperReviewersMatchingMessage.DeserializeAsync(matchingEntry.Data);

            if (matchingMessage.ReviewCommitment.SequenceEqual(reviewCommitmentBytes))
            {
                return matchingMessage;
            }
        }

        throw new MessageCreationException(ProtocolStep.PaperReviewersMatching);
    }
}
