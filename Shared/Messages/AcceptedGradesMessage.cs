namespace Apachi.Shared.Messages;

// 17: {|G_a|}K_PC^-1
public class AcceptedGradesMessage : IMessage
{
    public required List<byte[]> Grades { get; init; }

    public async Task<byte[]> SerializeAsync()
    {
        var serializedGrades = SerializeByteArrays(Grades);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(serializedGrades, pcPrivateKey);

        var serialized = SerializeByteArrays(serializedGrades, signature);
        return serialized;
    }

    public static async Task<AcceptedGradesMessage> DeserializeAsync(byte[] data)
    {
        var (serializedGrades, signature) = DeserializeTwoByteArrays(data);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(serializedGrades, signature, pcPublicKey);

        var grades = DeserializeByteArrays(serializedGrades);
        var message = new AcceptedGradesMessage { Grades = grades };
        return message;
    }
}
