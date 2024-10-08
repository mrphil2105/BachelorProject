using System.Runtime.Serialization;
using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<ReviewMessage?> GetReviewMessageByPaperHashAsync(
        byte[] paperHash,
        byte[] sharedKey,
        byte[] reviewerPublicKey
    )
    {
        var pcPrivateKey = GetPCPrivateKey();
        var reviewEntries = EnumerateEntriesAsync(ProtocolStep.Review);

        await foreach (var reviewEntry in reviewEntries)
        {
            ReviewMessage reviewMessage;

            try
            {
                reviewMessage = await ReviewMessage.DeserializeAsync(reviewEntry.Data, sharedKey, reviewerPublicKey);
            }
            catch (Exception exception) when (exception is CryptographicException or SerializationException)
            {
                continue;
            }

            var messagePaperHash = SHA256.HashData(reviewMessage.Paper);

            if (!messagePaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            return reviewMessage;
        }

        return null;
    }
}
