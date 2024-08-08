namespace Apachi.Shared.Messages;

// 16: {|C(P,r_r);g;r_g|}K_PC^-1
public class PaperRejectionMessage : IMessage
{
    public required byte[] ReviewCommitment { get; init; }

    public required byte[] Grade { get; init; }

    public required byte[] GradeRandomness { get; init; }

    public async Task<byte[]> SerializeAsync()
    {
        var commitment_Grade_Randomness = SerializeByteArrays(ReviewCommitment, Grade, GradeRandomness);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(commitment_Grade_Randomness, pcPrivateKey);

        var serialized = SerializeByteArrays(commitment_Grade_Randomness, signature);
        return serialized;
    }

    public static async Task<PaperRejectionMessage> DeserializeAsync(byte[] data)
    {
        var (commitment_Grade_Randomness, signature) = DeserializeTwoByteArrays(data);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(commitment_Grade_Randomness, signature, pcPublicKey);

        var (reviewCommitment, grade, gradeRandomness) = DeserializeThreeByteArrays(commitment_Grade_Randomness);
        var message = new PaperRejectionMessage
        {
            ReviewCommitment = reviewCommitment,
            Grade = grade,
            GradeRandomness = gradeRandomness
        };
        return message;
    }
}
