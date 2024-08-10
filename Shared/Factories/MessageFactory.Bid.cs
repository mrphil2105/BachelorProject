using System.Runtime.Serialization;
using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<BidMessage?> GetBidMessageByPaperHashAsync(
        byte[] paperHash,
        byte[] sharedKey,
        byte[] reviewerPublicKey
    )
    {
        var bidEntries = EnumerateEntriesAsync(ProtocolStep.Bid);

        await foreach (var bidEntry in bidEntries)
        {
            BidMessage bidMessage;

            try
            {
                bidMessage = await BidMessage.DeserializeAsync(bidEntry.Data, sharedKey, reviewerPublicKey);
            }
            catch (Exception exception) when (exception is CryptographicException or SerializationException)
            {
                continue;
            }

            var messagePaperHash = await Task.Run(() => SHA256.HashData(bidMessage.Paper));

            if (!messagePaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            return bidMessage;
        }

        return null;
    }
}
