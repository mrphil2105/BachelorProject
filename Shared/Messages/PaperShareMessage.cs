namespace Apachi.Shared.Messages;

// 4: {|{|P|}K_PC^-1|}K_PCR
public class PaperShareMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] sharedKey)
    {
        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(Paper, pcPrivateKey);

        var paper_Signature = SerializeByteArrays(Paper, signature);
        var encrypted = await SymmetricEncryptAsync(paper_Signature, sharedKey);
        return encrypted;
    }

    public static async Task<PaperShareMessage> DeserializeAsync(byte[] data, byte[] sharedKey)
    {
        var paper_Signature = await SymmetricDecryptAsync(data, sharedKey);
        var (paper, signature) = DeserializeTwoByteArrays(paper_Signature);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(paper, signature, pcPublicKey);

        var message = new PaperShareMessage { Paper = paper };
        return message;
    }
}
