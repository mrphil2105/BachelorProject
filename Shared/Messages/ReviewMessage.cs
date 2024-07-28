namespace Apachi.Shared.Messages;

// 8: {|{|W|}K_R^-1|}K_PCR
public class ReviewMessage : IMessage
{
    public required byte[] Review { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey, byte[] sharedKey)
    {
        var signature = await CalculateSignatureAsync(Review, reviewerPrivateKey);

        var reviewAndSignature = SerializeByteArrays(Review, signature);
        var encryptedReviewAndSignature = await SymmetricEncryptAsync(reviewAndSignature, sharedKey);
        return encryptedReviewAndSignature;
    }

    public static async Task<ReviewMessage> DeserializeAsync(byte[] data, byte[] sharedKey, byte[] reviewerPublicKey)
    {
        var reviewAndSignature = await SymmetricDecryptAsync(data, sharedKey);
        var (review, signature) = DeserializeTwoByteArrays(reviewAndSignature);

        await ThrowIfInvalidSignatureAsync(review, signature, reviewerPublicKey);

        var message = new ReviewMessage { Review = review };
        return message;
    }
}
