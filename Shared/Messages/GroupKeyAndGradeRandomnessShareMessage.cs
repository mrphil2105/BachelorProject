namespace Apachi.Shared.Messages;

// 10: {|{|P;K_P;r_g|}K_PC^-1|}K_PCR
public class GroupKeyAndGradeRandomnessShareMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] GroupKey { get; init; }

    public required byte[] GradeRandomness { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] sharedKey)
    {
        var paperAndGroupKeyAndRandomness = SerializeByteArrays(Paper, GroupKey, GradeRandomness);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(paperAndGroupKeyAndRandomness, pcPrivateKey);

        var paperAndGroupKeyAndRandomnessAndSignature = SerializeByteArrays(paperAndGroupKeyAndRandomness, signature);
        var encryptedPaperAndGroupKeyAndRandomnessAndSignature = await SymmetricEncryptAsync(
            paperAndGroupKeyAndRandomnessAndSignature,
            sharedKey
        );
        return encryptedPaperAndGroupKeyAndRandomnessAndSignature;
    }

    public static async Task<GroupKeyAndGradeRandomnessShareMessage> DeserializeAsync(byte[] data, byte[] sharedKey)
    {
        var paperAndGroupKeyAndRandomnessAndSignature = await SymmetricDecryptAsync(data, sharedKey);
        var (paperAndGroupKeyAndRandomness, signature) = DeserializeTwoByteArrays(
            paperAndGroupKeyAndRandomnessAndSignature
        );

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(paperAndGroupKeyAndRandomness, signature, pcPublicKey);

        var (paper, groupKey, gradeRandomness) = DeserializeThreeByteArrays(paperAndGroupKeyAndRandomness);
        var message = new GroupKeyAndGradeRandomnessShareMessage
        {
            Paper = paper,
            GroupKey = groupKey,
            GradeRandomness = gradeRandomness
        };
        return message;
    }
}
