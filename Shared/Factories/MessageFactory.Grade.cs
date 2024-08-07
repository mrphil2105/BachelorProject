using System.Security.Cryptography;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<GradeMessage?> GetGradeMessageByGroupKeyAsync(byte[] groupKey, byte[] reviewerPublicKey)
    {
        var gradeEntries = await _logDbContext.Entries.Where(entry => entry.Step == ProtocolStep.Grade).ToListAsync();

        foreach (var gradeEntry in gradeEntries)
        {
            try
            {
                var gradeMessage = await GradeMessage.DeserializeAsync(gradeEntry.Data, groupKey, reviewerPublicKey);
                return gradeMessage;
            }
            catch (CryptographicException)
            {
                continue;
            }
        }

        return null;
    }
}
