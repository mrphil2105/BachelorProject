namespace Apachi.Shared.Messages;

// 14: {|{|g|}K_R^-1|}K_P
public class GradeMessage : IMessage
{
    public required byte[] Grade { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey, byte[] groupKey)
    {
        var signature = await CalculateSignatureAsync(Grade, reviewerPrivateKey);

        var grade_Signature = SerializeByteArrays(Grade, signature);
        var encrypted = await SymmetricEncryptAsync(grade_Signature, groupKey);
        return encrypted;
    }

    public static async Task<GradeMessage> DeserializeAsync(byte[] data, byte[] groupKey, byte[] reviewerPublicKey)
    {
        var grade_Signature = await SymmetricDecryptAsync(data, groupKey);
        var (grade, signature) = DeserializeTwoByteArrays(grade_Signature);

        await ThrowIfInvalidSignatureAsync(grade, signature, reviewerPublicKey);

        var message = new GradeMessage { Grade = grade };
        return message;
    }
}
