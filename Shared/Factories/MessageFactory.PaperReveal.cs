using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<PaperRevealMessage?> GetRevealMessageByPaperHashAsync(byte[] paperHash)
    {
        var revealEntries = EnumerateEntriesAsync(ProtocolStep.PaperReveal);

        await foreach (var revealEntry in revealEntries)
        {
            PaperRevealMessage revealMessage;

            try
            {
                revealMessage = await PaperRevealMessage.DeserializeAsync(revealEntry.Data);
            }
            catch (CryptographicException)
            {
                continue;
            }

            var messagePaperHash = await Task.Run(() => SHA256.HashData(revealMessage.Paper));

            if (!messagePaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            return revealMessage;
        }

        return null;
    }
}
