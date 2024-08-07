using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async IAsyncEnumerable<(
        ReviewCommitmentAndNonceSignatureMessage SignatureMessage,
        byte[] ReviewerPublicKey
    )> GetCommitmentAndNonceSignatureMessagesAsync(List<byte[]> reviewerPublicKeys)
    {
        var signatureEntries = await GetEntriesAsync(ProtocolStep.ReviewCommitmentAndNonceSignature);

        foreach (var signatureEntry in signatureEntries)
        {
            foreach (var reviewerPublicKey in reviewerPublicKeys)
            {
                ReviewCommitmentAndNonceSignatureMessage signatureMessage;

                try
                {
                    signatureMessage = await ReviewCommitmentAndNonceSignatureMessage.DeserializeAsync(
                        signatureEntry.Data,
                        reviewerPublicKey
                    );
                }
                catch (CryptographicException)
                {
                    continue;
                }

                yield return (signatureMessage, reviewerPublicKey);
            }
        }
    }
}
