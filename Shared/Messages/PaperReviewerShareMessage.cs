namespace Apachi.Shared.Messages;

// 4: {|{|P|}K_PC^-1|}K_PCR
public class PaperReviewerShareMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] sharedKey)
    {
        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(Paper, pcPrivateKey);

        var paperAndSignature = SerializeByteArrays(Paper, signature);
        var encryptedPaperAndSignature = await SymmetricEncryptAsync(paperAndSignature, sharedKey);
        return encryptedPaperAndSignature;
    }

    public static async Task<PaperReviewerShareMessage> DeserializeAsync(byte[] data, byte[] sharedKey)
    {
        var paperAndSignature = await SymmetricDecryptAsync(data, sharedKey);
        var (paper, signature) = DeserializeTwoByteArrays(paperAndSignature);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(paper, signature, pcPublicKey);

        var message = new PaperReviewerShareMessage { Paper = paper };
        return message;
    }
}
