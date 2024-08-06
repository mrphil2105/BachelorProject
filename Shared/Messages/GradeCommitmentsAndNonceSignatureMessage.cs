namespace Apachi.Shared.Messages;

// 13: {|C(P,r_r);C(g,r_g);n_r|}K_R^-1
public class GradeCommitmentsAndNonceSignatureMessage : IMessage
{
    public required byte[] ReviewCommitment { get; init; }

    public required byte[] GradeCommitment { get; init; }

    public required byte[] ReviewNonce { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey)
    {
        var reviewCommitment_GradeCommitment_Nonce = SerializeByteArrays(
            ReviewCommitment,
            GradeCommitment,
            ReviewNonce
        );
        var signature = await CalculateSignatureAsync(reviewCommitment_GradeCommitment_Nonce, reviewerPrivateKey);

        var serialized = SerializeByteArrays(reviewCommitment_GradeCommitment_Nonce, signature);
        return serialized;
    }

    public static async Task<GradeCommitmentsAndNonceSignatureMessage> DeserializeAsync(
        byte[] data,
        byte[] reviewerPublicKey
    )
    {
        var (reviewCommitment_GradeCommitment_Nonce, signature) = DeserializeTwoByteArrays(data);
        await ThrowIfInvalidSignatureAsync(reviewCommitment_GradeCommitment_Nonce, signature, reviewerPublicKey);

        var (reviewCommitment, gradeCommitment, reviewNonce) = DeserializeThreeByteArrays(
            reviewCommitment_GradeCommitment_Nonce
        );

        var message = new GradeCommitmentsAndNonceSignatureMessage
        {
            ReviewCommitment = reviewCommitment,
            GradeCommitment = gradeCommitment,
            ReviewNonce = reviewNonce
        };
        return message;
    }
}
