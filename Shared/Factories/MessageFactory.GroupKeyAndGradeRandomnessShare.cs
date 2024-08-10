using System.Runtime.Serialization;
using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<GroupKeyAndGradeRandomnessShareMessage?> GetGroupKeyAndRandomnessMessageByPaperHashAsync(
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

        return null;
    }

    public async IAsyncEnumerable<GroupKeyAndGradeRandomnessShareMessage> GetGroupKeyAndRandomnessMessagesAsync(
        byte[] sharedKey
    )
    {
        var groupKeyEntries = EnumerateEntriesAsync(ProtocolStep.GroupKeyAndGradeRandomnessShare);

        await foreach (var groupKeyEntry in groupKeyEntries)
        {
            GroupKeyAndGradeRandomnessShareMessage groupKeyMessage;

            try
            {
                groupKeyMessage = await GroupKeyAndGradeRandomnessShareMessage.DeserializeAsync(
                    groupKeyEntry.Data,
                    sharedKey
                );
            }
            catch (Exception exception) when (exception is CryptographicException or SerializationException)
            {
                continue;
            }

            yield return groupKeyMessage;
        }
    }
}
