namespace Apachi.Shared.Messages;

// 9: {|C(P,r_r);n_r|}K_R^-1
public class ReviewCommitmentAndNonceSignatureMessage : IMessage
{
    public required byte[] ReviewCommitment { get; init; }

    public required byte[] ReviewNonce { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey)
    {
        var reviewCommitment_Nonce = SerializeByteArrays(ReviewCommitment, ReviewNonce);
        var signature = await CalculateSignatureAsync(reviewCommitment_Nonce, reviewerPrivateKey);

        var serialized = SerializeByteArrays(reviewCommitment_Nonce, signature);
        return serialized;
    }

    public static async Task<ReviewCommitmentAndNonceSignatureMessage> DeserializeAsync(
        byte[] data,
        byte[] reviewerPublicKey
    )
    {
        var (reviewCommitment_Nonce, signature) = DeserializeTwoByteArrays(data);
        await ThrowIfInvalidSignatureAsync(reviewCommitment_Nonce, signature, reviewerPublicKey);

        var (reviewCommitment, reviewNonce) = DeserializeTwoByteArrays(reviewCommitment_Nonce);
        var message = new ReviewCommitmentAndNonceSignatureMessage
        {
            ReviewCommitment = reviewCommitment,
            ReviewNonce = reviewNonce
        };
        return message;
    }
}
