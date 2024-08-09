namespace Apachi.Shared.Messages;

// 11: {|{|{{|W_j|}K_R_j^-1..{|W_m|}K_R_m^-1}|}K_PC^-1|}K_P
public class ReviewsShareMessage : IMessage
{
    public required List<byte[]> Reviews { get; init; }

    public List<byte[]>? ReviewSignatures { get; private init; }

    public async Task<byte[]> SerializeAsync(IEnumerable<byte[]> signatures, byte[] groupKey)
    {
        var serializedReviews = SerializeByteArrays(Reviews);
        var serializedSignatures = SerializeByteArrays(signatures);
        var reviews_Signatures = SerializeByteArrays(serializedReviews, serializedSignatures);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(reviews_Signatures, pcPrivateKey);

        var reviews_Signatures_Signature = SerializeByteArrays(reviews_Signatures, signature);
        var encrypted = await SymmetricEncryptAsync(reviews_Signatures_Signature, groupKey);
        return encrypted;
    }

    public static async Task<ReviewsShareMessage> DeserializeAsync(
        byte[] data,
        byte[] groupKey,
        IEnumerable<byte[]> reviewerPublicKeys
    )
    {
        var reviews_Signatures_Signature = await SymmetricDecryptAsync(data, groupKey);
        var (reviews_Signatures, signature) = DeserializeTwoByteArrays(reviews_Signatures_Signature);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(reviews_Signatures, signature, pcPublicKey);

        var (serializedReviews, serializedSignatures) = DeserializeTwoByteArrays(reviews_Signatures);
        var reviews = DeserializeByteArrays(serializedReviews);
        var signatures = DeserializeByteArrays(serializedSignatures);

        var index = 0;

        foreach (var reviewerPublicKey in reviewerPublicKeys)
        {
            await ThrowIfInvalidSignatureAsync(reviews[index], signatures[index], reviewerPublicKey);
            index++;
        }

        var message = new ReviewsShareMessage { Reviews = reviews, ReviewSignatures = signatures };
        return message;
    }
}
