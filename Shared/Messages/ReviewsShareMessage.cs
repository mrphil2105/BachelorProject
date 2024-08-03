namespace Apachi.Shared.Messages;

// 11: {|{|{{|W_j|}K_R_j^-1..{|W_m|}K_R_m^-1}|}K_PC^-1|}K_P
public class ReviewsShareMessage : IMessage
{
    public required List<byte[]> Reviews { get; init; }

    public async Task<byte[]> SerializeAsync(IEnumerable<byte[]> signatures, byte[] groupKey)
    {
        var serializedReviews = SerializeByteArrays(Reviews);
        var serializedSignatures = SerializeByteArrays(signatures);
        var reviewsAndSignatures = SerializeByteArrays(serializedReviews, serializedSignatures);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(reviewsAndSignatures, pcPrivateKey);

        var reviewsAndSignaturesAndSignature = SerializeByteArrays(reviewsAndSignatures, signature);
        var encryptedReviewsAndSignaturesAndSignature = await SymmetricEncryptAsync(
            reviewsAndSignaturesAndSignature,
            groupKey
        );
        return encryptedReviewsAndSignaturesAndSignature;
    }

    public static async Task<ReviewsShareMessage> DeserializeAsync(
        byte[] data,
        byte[] groupKey,
        IEnumerable<byte[]> reviewerPublicKeys
    )
    {
        var reviewsAndSignaturesAndSignature = await SymmetricDecryptAsync(data, groupKey);
        var (reviewsAndSignatures, signature) = DeserializeTwoByteArrays(reviewsAndSignaturesAndSignature);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(reviewsAndSignatures, signature, pcPublicKey);

        var (serializedReviews, serializedSignatures) = DeserializeTwoByteArrays(reviewsAndSignatures);
        var reviews = DeserializeByteArrays(serializedReviews);
        var signatures = DeserializeByteArrays(serializedSignatures);

        var index = 0;

        foreach (var reviewerPublicKey in reviewerPublicKeys)
        {
            await ThrowIfInvalidSignatureAsync(reviews[index], signatures[index], reviewerPublicKey);
            index++;
        }

        var message = new ReviewsShareMessage { Reviews = reviews };
        return message;
    }
}
