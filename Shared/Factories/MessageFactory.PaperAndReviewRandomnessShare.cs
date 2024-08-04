using System.Security.Cryptography;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<PaperAndReviewRandomnessShareMessage> GetPaperAndRandomnessMessageByPaperHashAsync(
        byte[] paperHash,
        byte[] sharedKey
    )
    {
        var paperEntryIds = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.PaperAndReviewRandomnessShare)
            .Select(entry => entry.Id)
            .ToListAsync();

        foreach (var paperEntryId in paperEntryIds)
        {
            var paperEntry = await _logDbContext.Entries.SingleAsync(entry => entry.Id == paperEntryId);
            PaperAndReviewRandomnessShareMessage paperMessage;

            try
            {
                paperMessage = await PaperAndReviewRandomnessShareMessage.DeserializeAsync(paperEntry.Data, sharedKey);
            }
            catch (CryptographicException)
            {
                continue;
            }

            var messagePaperHash = await Task.Run(() => SHA256.HashData(paperMessage.Paper));

            if (!messagePaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            return paperMessage;
        }

        throw new MessageCreationException(ProtocolStep.PaperAndReviewRandomnessShare);
    }
}
