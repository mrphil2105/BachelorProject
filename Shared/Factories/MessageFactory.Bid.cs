using System.Security.Cryptography;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<BidMessage?> GetBidMessageByPaperHashAsync(
        byte[] paperHash,
        byte[] sharedKey,
        byte[] reviewerPublicKey
    )
    {
        var bidEntryIds = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.Bid)
            .Select(entry => entry.Id)
            .ToListAsync();

        foreach (var bidEntryId in bidEntryIds)
        {
            var bidEntry = await _logDbContext.Entries.SingleAsync(entry => entry.Id == bidEntryId);
            BidMessage bidMessage;

            try
            {
                bidMessage = await BidMessage.DeserializeAsync(bidEntry.Data, sharedKey, reviewerPublicKey);
            }
            catch (CryptographicException)
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
