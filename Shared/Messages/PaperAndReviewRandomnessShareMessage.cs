namespace Apachi.Shared.Messages;

// 7: {|{|P;r_r|}K_PC^-1|}K_PCR
public class PaperAndReviewRandomnessShareMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] ReviewRandomness { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] sharedKey)
    {
        var paper_Randomness = SerializeByteArrays(Paper, ReviewRandomness);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(paper_Randomness, pcPrivateKey);

        var paper_Randomness_Signature = SerializeByteArrays(paper_Randomness, signature);
        var encrypted = await SymmetricEncryptAsync(paper_Randomness_Signature, sharedKey);
        return encrypted;
    }

    public static async Task<PaperAndReviewRandomnessShareMessage> DeserializeAsync(byte[] data, byte[] sharedKey)
    {
        var paper_Randomness_Signature = await SymmetricDecryptAsync(data, sharedKey);
        var (paper_Randomness, signature) = DeserializeTwoByteArrays(paper_Randomness_Signature);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(paper_Randomness, signature, pcPublicKey);

        var (paper, reviewRandomness) = DeserializeTwoByteArrays(paper_Randomness);
        var message = new PaperAndReviewRandomnessShareMessage { Paper = paper, ReviewRandomness = reviewRandomness };
        return message;
    }
}
