using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<PaperClaimMessage?> GetClaimMessageBySubmissionPublicKeyAsync(byte[] submissionPublicKey)
    {
        var claimEntries = EnumerateEntriesAsync(ProtocolStep.PaperClaim);

        await foreach (var claimEntry in claimEntries)
        {
            try
            {
                var claimMessage = await PaperClaimMessage.DeserializeAsync(claimEntry.Data, submissionPublicKey);
                return claimMessage;
            }
            catch (CryptographicException)
            {
                continue;
            }
        }

        return null;
    }
}
