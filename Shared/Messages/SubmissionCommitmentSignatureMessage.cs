namespace Apachi.Shared.Messages;

// 3: {|C(P,r_s)|}K_PC^-1
public class SubmissionCommitmentSignatureMessage : IMessage
{
    public required byte[] SubmissionCommitment { get; init; }

    public async Task<byte[]> SerializeAsync()
    {
        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(SubmissionCommitment, pcPrivateKey);

        var serialized = SerializeByteArrays(SubmissionCommitment, signature);
        return serialized;
    }

    public static async Task<SubmissionCommitmentSignatureMessage> DeserializeAsync(byte[] data)
    {
        var (submissionCommitment, signature) = DeserializeTwoByteArrays(data);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(submissionCommitment, signature, pcPublicKey);

        var message = new SubmissionCommitmentSignatureMessage { SubmissionCommitment = submissionCommitment };
        return message;
    }
}
