namespace Apachi.Shared.Messages;

// 2: {|C(P,r_s);C(S,r_i)|}K_S^-1;K_S
public class SubmissionCommitmentsAndPublicKeyMessage : IMessage
{
    public required byte[] SubmissionCommitment { get; init; }

    public required byte[] IdentityCommitment { get; init; }

    public required byte[] SubmissionPublicKey { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] submissionPrivateKey)
    {
        var commitments = SerializeByteArrays(SubmissionCommitment, IdentityCommitment);
        var signature = await CalculateSignatureAsync(commitments, submissionPrivateKey);

        var serialized = SerializeByteArrays(commitments, signature, SubmissionPublicKey);
        return serialized;
    }

    public static async Task<SubmissionCommitmentsAndPublicKeyMessage> DeserializeAsync(byte[] data)
    {
        var (commitments, signature, submissionPublicKey) = DeserializeThreeByteArrays(data);
        await ThrowIfInvalidSignatureAsync(commitments, signature, submissionPublicKey);

        var (submissionCommitment, identityCommitment) = DeserializeTwoByteArrays(commitments);
        var message = new SubmissionCommitmentsAndPublicKeyMessage
        {
            SubmissionCommitment = submissionCommitment,
            IdentityCommitment = identityCommitment,
            SubmissionPublicKey = submissionPublicKey
        };
        return message;
    }
}
