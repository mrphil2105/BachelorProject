namespace Apachi.Shared.Messages;

// 15: {|{|g;{{|W_j|}K_R_j^-1..{|W_m|}K_R_m^-1}|}K_PC^-1|}K_P
public class GradeAndReviewsShareMessage : IMessage
{
    public required byte[] Grade { get; init; }

    public required List<byte[]> Reviews { get; init; }

    public List<byte[]>? ReviewSignatures { get; private init; }

    public async Task<byte[]> SerializeAsync(IEnumerable<byte[]> signatures, byte[] submissionKey)
    {
        var serializedReviews = SerializeByteArrays(Reviews);
        var serializedSignatures = SerializeByteArrays(signatures);
        var reviews_Signatures = SerializeByteArrays(serializedReviews, serializedSignatures);

        var grade_Reviews_Signatures = SerializeByteArrays(Grade, reviews_Signatures);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(grade_Reviews_Signatures, pcPrivateKey);

        var grade_Reviews_Signatures_Signature = SerializeByteArrays(grade_Reviews_Signatures, signature);
        var encrypted = await SymmetricEncryptAsync(grade_Reviews_Signatures_Signature, submissionKey);
        return encrypted;
    }

    public static async Task<GradeAndReviewsShareMessage> DeserializeAsync(
        byte[] data,
        byte[] submissionKey,
        IEnumerable<byte[]> reviewerPublicKeys
    )
    {
        var grade_Reviews_Signatures_Signature = await SymmetricDecryptAsync(data, submissionKey);
        var (grade_Reviews_Signatures, signature) = DeserializeTwoByteArrays(grade_Reviews_Signatures_Signature);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(grade_Reviews_Signatures, signature, pcPublicKey);

        var (grade, reviews_Signatures) = DeserializeTwoByteArrays(grade_Reviews_Signatures);

        var (serializedReviews, serializedSignatures) = DeserializeTwoByteArrays(reviews_Signatures);
        var reviews = DeserializeByteArrays(serializedReviews);
        var signatures = DeserializeByteArrays(serializedSignatures);

        var index = 0;

        foreach (var reviewerPublicKey in reviewerPublicKeys)
        {
            await ThrowIfInvalidSignatureAsync(reviews[index], signatures[index], reviewerPublicKey);
            index++;
        }

        var message = new GradeAndReviewsShareMessage
        {
            Grade = grade,
            Reviews = reviews,
            ReviewSignatures = signatures
        };
        return message;
    }
}
