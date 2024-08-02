namespace Apachi.Shared.Messages;

// 8: {|{|P;W|}K_R^-1|}K_PCR
public class ReviewMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] Review { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey, byte[] sharedKey)
    {
        var paperAndReview = SerializeByteArrays(Paper, Review);
        var signature = await CalculateSignatureAsync(paperAndReview, reviewerPrivateKey);

        var paperAndReviewAndSignature = SerializeByteArrays(paperAndReview, signature);
        var encryptedPaperAndReviewAndSignature = await SymmetricEncryptAsync(paperAndReviewAndSignature, sharedKey);
        return encryptedPaperAndReviewAndSignature;
    }

    public static async Task<ReviewMessage> DeserializeAsync(byte[] data, byte[] sharedKey, byte[] reviewerPublicKey)
    {
        var paperAndReviewAndSignature = await SymmetricDecryptAsync(data, sharedKey);
        var (paperAndReview, signature) = DeserializeTwoByteArrays(paperAndReviewAndSignature);

        await ThrowIfInvalidSignatureAsync(paperAndReview, signature, reviewerPublicKey);
        var (paper, review) = DeserializeTwoByteArrays(paperAndReview);

        var message = new ReviewMessage { Paper = paper, Review = review };
        return message;
    }
}
