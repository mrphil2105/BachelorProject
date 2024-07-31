namespace Apachi.Shared.Messages;

// 9: {|C(P,r_r);n_r|}K_R^-1
public class ReviewCommitmentAndNonceSignatureMessage : IMessage
{
    public required byte[] ReviewCommitment { get; init; }

    public required byte[] ReviewNonce { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey)
    {
        var reviewCommitmentAndNonce = SerializeByteArrays(ReviewCommitment, ReviewNonce);
        var signature = await CalculateSignatureAsync(reviewCommitmentAndNonce, reviewerPrivateKey);

        var serialized = SerializeByteArrays(reviewCommitmentAndNonce, signature);
        return serialized;
    }

    public static async Task<ReviewCommitmentAndNonceSignatureMessage> DeserializeAsync(
        byte[] data,
        byte[] reviewerPublicKey
    )
    {
        var (reviewCommitmentAndNonce, signature) = DeserializeTwoByteArrays(data);
        await ThrowIfInvalidSignatureAsync(reviewCommitmentAndNonce, signature, reviewerPublicKey);

        var (reviewCommitment, reviewNonce) = DeserializeTwoByteArrays(reviewCommitmentAndNonce);
        var message = new ReviewCommitmentAndNonceSignatureMessage
        {
            ReviewCommitment = reviewCommitment,
            ReviewNonce = reviewNonce
        };
        return message;
    }
}
