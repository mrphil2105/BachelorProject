using System.Security.Cryptography;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<GroupKeyAndGradeRandomnessShareMessage> GetGroupKeyAndRandomnessMessageByPaperHashAsync(
        byte[] paperHash,
        byte[] sharedKey
    )
    {
        var groupKeyMessages = GetGroupKeyAndRandomnessMessagesAsync(sharedKey);

        await foreach (var groupKeyMessage in groupKeyMessages)
        {
            var messagePaperHash = await Task.Run(() => SHA256.HashData(groupKeyMessage.Paper));

            if (!messagePaperHash.SequenceEqual(paperHash))
            {
                continue;
            }

            return groupKeyMessage;
        }

        throw new MessageCreationException(ProtocolStep.GroupKeyAndGradeRandomnessShare);
    }

    public async IAsyncEnumerable<GroupKeyAndGradeRandomnessShareMessage> GetGroupKeyAndRandomnessMessagesAsync(
        byte[] sharedKey
    )
    {
        var groupKeyEntryIds = await _logDbContext
            .Entries.Where(entry => entry.Step == ProtocolStep.GroupKeyAndGradeRandomnessShare)
            .Select(entry => entry.Id)
            .ToListAsync();

        foreach (var groupKeyEntryId in groupKeyEntryIds)
        {
            var groupKeyEntry = await _logDbContext.Entries.SingleAsync(entry => entry.Id == groupKeyEntryId);
            GroupKeyAndGradeRandomnessShareMessage groupKeyMessage;

            try
            {
                groupKeyMessage = await GroupKeyAndGradeRandomnessShareMessage.DeserializeAsync(
                    groupKeyEntry.Data,
                    sharedKey
                );
            }
            catch (CryptographicException)
            {
                continue;
            }

            yield return groupKeyMessage;
        }
    }
}
