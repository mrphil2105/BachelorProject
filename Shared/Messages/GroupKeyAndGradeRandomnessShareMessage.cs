namespace Apachi.Shared.Messages;

// 10: {|{|P;K_P;r_g|}K_PC^-1|}K_PCR
public class GroupKeyAndGradeRandomnessShareMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] GroupKey { get; init; }

    public required byte[] GradeRandomness { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] sharedKey)
    {
        var paper_GroupKey_Randomness = SerializeByteArrays(Paper, GroupKey, GradeRandomness);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(paper_GroupKey_Randomness, pcPrivateKey);

        var paper_GroupKey_Randomness_Signature = SerializeByteArrays(paper_GroupKey_Randomness, signature);
        var encrypted = await SymmetricEncryptAsync(paper_GroupKey_Randomness_Signature, sharedKey);
        return encrypted;
    }

    public static async Task<GroupKeyAndGradeRandomnessShareMessage> DeserializeAsync(byte[] data, byte[] sharedKey)
    {
        var paper_GroupKey_Randomness_Signature = await SymmetricDecryptAsync(data, sharedKey);
        var (paper_GroupKey_Randomness, signature) = DeserializeTwoByteArrays(paper_GroupKey_Randomness_Signature);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(paper_GroupKey_Randomness, signature, pcPublicKey);

        var (paper, groupKey, gradeRandomness) = DeserializeThreeByteArrays(paper_GroupKey_Randomness);
        var message = new GroupKeyAndGradeRandomnessShareMessage
        {
            Paper = paper,
            GroupKey = groupKey,
            GradeRandomness = gradeRandomness
        };
        return message;
    }
}
