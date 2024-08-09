using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async IAsyncEnumerable<(
        DiscussionMessage DiscussionMessage,
        byte[] ReviewerPublicKey
    )> GetDiscussionMessagesByGroupKeyAsync(byte[] groupKey, List<byte[]> reviewerPublicKeys)
    {
        var discussionEntries = await GetEntriesAsync(ProtocolStep.Discussion);

        foreach (var discussionEntry in discussionEntries)
        {
            foreach (var reviewerPublicKey in reviewerPublicKeys)
            {
                DiscussionMessage discussionMessage;

                try
                {
                    discussionMessage = await DiscussionMessage.DeserializeAsync(
                        discussionEntry.Data,
                        groupKey,
                        reviewerPublicKey
                    );
                }
                catch (CryptographicException)
                {
                    continue;
                }

                yield return (discussionMessage, reviewerPublicKey);
            }
        }
    }
}
