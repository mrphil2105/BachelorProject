using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<PaperAndReviewRandomnessShareMessage> GetPaperAndRandomnessMessageByPaperHashAsync(
        byte[] paperHash,
        byte[] sharedKey
    )
    {
        var paperMessages = GetPaperAndRandomnessMessagesAsync(sharedKey);

        await foreach (var paperMessage in paperMessages)
        {
            var messagePaperHash = await Task.Run(() => SHA256.HashData(paperMessage.Paper));

            if (!messagePaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            return paperMessage;
        }

        throw new MessageCreationException(ProtocolStep.PaperAndReviewRandomnessShare);
    }

    public async IAsyncEnumerable<PaperAndReviewRandomnessShareMessage> GetPaperAndRandomnessMessagesAsync(
        byte[] sharedKey
    )
    {
        var paperEntries = EnumerateEntriesAsync(ProtocolStep.PaperAndReviewRandomnessShare);

        await foreach (var paperEntry in paperEntries)
        {
            PaperAndReviewRandomnessShareMessage paperMessage;

            try
            {
                paperMessage = await PaperAndReviewRandomnessShareMessage.DeserializeAsync(paperEntry.Data, sharedKey);
            }
            catch (CryptographicException)
            {
                continue;
            }

            yield return paperMessage;
        }
    }
}
