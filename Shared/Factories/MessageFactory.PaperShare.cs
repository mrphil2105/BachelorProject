using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<PaperShareMessage?> GetPaperMessageByPaperHashAsync(byte[] paperHash, byte[] sharedKey)
    {
        var paperMessages = GetPaperMessagesAsync(sharedKey);

        await foreach (var paperMessage in paperMessages)
        {
            var messagePaperHash = await Task.Run(() => SHA256.HashData(paperMessage.Paper));

            if (!messagePaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            return paperMessage;
        }

        return null;
    }

    public async IAsyncEnumerable<PaperShareMessage> GetPaperMessagesAsync(byte[] sharedKey)
    {
        var paperEntries = EnumerateEntriesAsync(ProtocolStep.PaperShare);

        await foreach (var paperEntry in paperEntries)
        {
            PaperShareMessage paperMessage;

            try
            {
                paperMessage = await PaperShareMessage.DeserializeAsync(paperEntry.Data, sharedKey);
            }
            catch (CryptographicException)
            {
                continue;
            }

            yield return paperMessage;
        }
    }
}
