namespace Apachi.Shared.Messages;

// 19: {|P;S;r_i|}K_S^-1
public class PaperClaimMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] Identity { get; init; }

    public required byte[] IdentityRandomness { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] submissionPrivateKey)
    {
        var paper_Identity_Randomness = SerializeByteArrays(Paper, Identity, IdentityRandomness);
        var signature = await CalculateSignatureAsync(paper_Identity_Randomness, submissionPrivateKey);

        var serialized = SerializeByteArrays(paper_Identity_Randomness, signature);
        return serialized;
    }

    public static async Task<PaperClaimMessage> DeserializeAsync(byte[] data, byte[] submissionPublicKey)
    {
        var (paper_Identity_Randomness, signature) = DeserializeTwoByteArrays(data);
        await ThrowIfInvalidSignatureAsync(paper_Identity_Randomness, signature, submissionPublicKey);

        var (paper, identity, identityRandomness) = DeserializeThreeByteArrays(paper_Identity_Randomness);
        var message = new PaperClaimMessage
        {
            Paper = paper,
            Identity = identity,
            IdentityRandomness = identityRandomness
        };
        return message;
    }
}
