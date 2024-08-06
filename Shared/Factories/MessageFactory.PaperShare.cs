using System.Security.Cryptography;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<PaperShareMessage> GetPaperMessageByPaperHashAsync(byte[] paperHash, byte[] sharedKey)
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

        throw new MessageCreationException(ProtocolStep.PaperShare);
    }

    public async IAsyncEnumerable<PaperShareMessage> GetPaperMessagesAsync(byte[] sharedKey)
    {
        var paperEntryIds = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.PaperShare)
            .Select(entry => entry.Id)
            .ToListAsync();

        foreach (var paperEntryId in paperEntryIds)
        {
            var paperEntry = await _logDbContext.Entries.SingleAsync(entry => entry.Id == paperEntryId);
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
