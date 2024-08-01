namespace Apachi.Shared.Messages;

// 7: {|{|P;r_r|}K_PC^-1|}K_PCR
public class PaperAndReviewRandomnessShareMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] ReviewRandomness { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] sharedKey)
    {
        var paperAndRandomness = SerializeByteArrays(Paper, ReviewRandomness);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(paperAndRandomness, pcPrivateKey);

        var paperAndRandomnessAndSignature = SerializeByteArrays(paperAndRandomness, signature);
        var encryptedPaperAndRandomnessAndSignature = await SymmetricEncryptAsync(
            paperAndRandomnessAndSignature,
            sharedKey
        );
        return encryptedPaperAndRandomnessAndSignature;
    }

    public static async Task<PaperAndReviewRandomnessShareMessage> DeserializeAsync(byte[] data, byte[] sharedKey)
    {
        var paperAndRandomnessAndSignature = await SymmetricDecryptAsync(data, sharedKey);
        var (paperAndRandomness, signature) = DeserializeTwoByteArrays(paperAndRandomnessAndSignature);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(paperAndRandomness, signature, pcPublicKey);

        var (paper, reviewRandomness) = DeserializeTwoByteArrays(paperAndRandomness);
        var message = new PaperAndReviewRandomnessShareMessage { Paper = paper, ReviewRandomness = reviewRandomness };
        return message;
    }
}
