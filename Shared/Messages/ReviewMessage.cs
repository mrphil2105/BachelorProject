namespace Apachi.Shared.Messages;

// 8: {|{|P;{|W|}K_R^-1|}K_R^-1|}K_PCR
public class ReviewMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] Review { get; init; }

    public byte[]? ReviewSignature { get; private init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey, byte[] sharedKey)
    {
        var reviewSignature = await CalculateSignatureAsync(Review, reviewerPrivateKey);
        var paper_Review_ReviewSignature = SerializeByteArrays(Paper, Review, reviewSignature);
        var signature = await CalculateSignatureAsync(paper_Review_ReviewSignature, reviewerPrivateKey);

        var paper_Review_ReviewSignature_Signature = SerializeByteArrays(paper_Review_ReviewSignature, signature);
        var encrypted = await SymmetricEncryptAsync(paper_Review_ReviewSignature_Signature, sharedKey);
        return encrypted;
    }

    public static async Task<ReviewMessage> DeserializeAsync(byte[] data, byte[] sharedKey, byte[] reviewerPublicKey)
    {
        var paper_Review_ReviewSignature_Signature = await SymmetricDecryptAsync(data, sharedKey);
        var (paper_Review_ReviewSignature, signature) = DeserializeTwoByteArrays(
            paper_Review_ReviewSignature_Signature
        );

        await ThrowIfInvalidSignatureAsync(paper_Review_ReviewSignature, signature, reviewerPublicKey);
        var (paper, review, reviewSignature) = DeserializeThreeByteArrays(paper_Review_ReviewSignature);
        await ThrowIfInvalidSignatureAsync(review, reviewSignature, reviewerPublicKey);

        var message = new ReviewMessage
        {
            Paper = paper,
            Review = review,
            ReviewSignature = reviewSignature
        };
        return message;
    }
}
