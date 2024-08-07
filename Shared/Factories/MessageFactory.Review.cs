using System.Security.Cryptography;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<ReviewMessage> GetReviewMessageByPaperHashAsync(
        byte[] paperHash,
        byte[] sharedKey,
        byte[] reviewerPublicKey
    )
    {
        var pcPrivateKey = GetPCPrivateKey();
        var reviewEntryIds = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.Review)
            .Select(entry => entry.Id)
            .ToListAsync();

        foreach (var reviewEntryId in reviewEntryIds)
        {
            var reviewEntry = await _logDbContext.Entries.SingleAsync(entry => entry.Id == reviewEntryId);
            ReviewMessage reviewMessage;

            try
            {
                reviewMessage = await ReviewMessage.DeserializeAsync(reviewEntry.Data, sharedKey, reviewerPublicKey);
            }
            catch (CryptographicException)
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

        throw new MessageCreationException(ProtocolStep.Review);
    }
}
