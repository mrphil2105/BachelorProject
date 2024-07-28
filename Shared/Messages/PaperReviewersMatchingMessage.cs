namespace Apachi.Shared.Messages;

// 6: {|C(P,r_r);{K_R_j..K_R_m};n_r|}K_PC^-1;NIZK{C(P,r_s)=C(P,r_r)}
public class PaperReviewersMatchingMessage : IMessage
{
    public required byte[] ReviewCommitment { get; init; }

    public required List<byte[]> ReviewerPublicKeys { get; init; }

    public required byte[] ReviewNonce { get; init; }

    public required byte[] EqualityProof { get; init; }

    public async Task<byte[]> SerializeAsync()
    {
        var serializedPublicKeys = SerializeByteArrays(ReviewerPublicKeys);
        var commitmentAndKeysAndNonce = SerializeByteArrays(ReviewCommitment, serializedPublicKeys, ReviewNonce);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(commitmentAndKeysAndNonce, pcPrivateKey);

        var serialized = SerializeByteArrays(commitmentAndKeysAndNonce, signature, EqualityProof);
        return serialized;
    }

    public static async Task<PaperReviewersMatchingMessage> DeserializeAsync(byte[] data)
    {
        var (commitmentAndKeysAndNonce, signature, equalityProof) = DeserializeThreeByteArrays(data);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(commitmentAndKeysAndNonce, signature, pcPublicKey);

        var (reviewCommitment, serializedPublicKeys, reviewNonce) = DeserializeThreeByteArrays(
            commitmentAndKeysAndNonce
        );
        var reviewerPublicKeys = DeserializeByteArrays(serializedPublicKeys);

        var message = new PaperReviewersMatchingMessage
        {
            ReviewCommitment = reviewCommitment,
            ReviewerPublicKeys = reviewerPublicKeys,
            ReviewNonce = reviewNonce,
            EqualityProof = equalityProof
        };
        return message;
    }
}
