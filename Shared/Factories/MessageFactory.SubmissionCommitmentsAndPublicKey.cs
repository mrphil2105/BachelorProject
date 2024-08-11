using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async IAsyncEnumerable<SubmissionCommitmentsAndPublicKeyMessage> GetCommitmentsAndPublicKeyMessagesAsync()
    {
        var publicKeyEntries = await GetEntriesAsync(ProtocolStep.SubmissionCommitmentsAndPublicKey);

        foreach (var publicKeyEntry in publicKeyEntries)
        {
            var publicKeyMessage = await SubmissionCommitmentsAndPublicKeyMessage.DeserializeAsync(publicKeyEntry.Data);
            yield return publicKeyMessage;
        }
    }
}
