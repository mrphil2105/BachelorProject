using System.Runtime.Serialization;
using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<ReviewsShareMessage?> GetReviewsMessageByGroupKeyAsync(
        byte[] groupKey,
        List<byte[]> reviewerPublicKeys
    )
    {
        var reviewsEntries = await GetEntriesAsync(ProtocolStep.ReviewsShare);

        foreach (var reviewsEntry in reviewsEntries)
        {
            try
            {
                var reviewsMessage = await ReviewsShareMessage.DeserializeAsync(
                    reviewsEntry.Data,
                    groupKey,
                    reviewerPublicKeys
                );
                return reviewsMessage;
            }
            catch (Exception exception) when (exception is CryptographicException or SerializationException)
            {
                continue;
            }
        }

        return null;
    }
}
