namespace Apachi.Shared.Messages;

// 8: {|{|P;W|}K_R^-1|}K_PCR
public class ReviewMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] Review { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey, byte[] sharedKey)
    {
        var paper_Review = SerializeByteArrays(Paper, Review);
        var signature = await CalculateSignatureAsync(paper_Review, reviewerPrivateKey);

        var paper_Review_Signature = SerializeByteArrays(paper_Review, signature);
        var encrypted = await SymmetricEncryptAsync(paper_Review_Signature, sharedKey);
        return encrypted;
    }

    public static async Task<ReviewMessage> DeserializeAsync(byte[] data, byte[] sharedKey, byte[] reviewerPublicKey)
    {
        var paper_Review_Signature = await SymmetricDecryptAsync(data, sharedKey);
        var (paper_Review, signature) = DeserializeTwoByteArrays(paper_Review_Signature);

        await ThrowIfInvalidSignatureAsync(paper_Review, signature, reviewerPublicKey);
        var (paper, review) = DeserializeTwoByteArrays(paper_Review);

        var message = new ReviewMessage { Paper = paper, Review = review };
        return message;
    }

    public static async Task<byte[]> DeserializeSignatureAsync(byte[] data, byte[] sharedKey)
    {
        var paper_Review_Signature = await SymmetricDecryptAsync(data, sharedKey);
        var (_, signature) = DeserializeTwoByteArrays(paper_Review_Signature);
        return signature;
    }
}
