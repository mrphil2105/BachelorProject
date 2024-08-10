namespace Apachi.Shared.Messages;

// 20: {|P;S;r_i|}K_PC^-1
public class PaperClaimConfirmationMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] Identity { get; init; }

    public required byte[] IdentityRandomness { get; init; }

    public async Task<byte[]> SerializeAsync()
    {
        var paper_Identity_Randomness = SerializeByteArrays(Paper, Identity, IdentityRandomness);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(paper_Identity_Randomness, pcPrivateKey);

        var serialized = SerializeByteArrays(paper_Identity_Randomness, signature);
        return serialized;
    }

    public static async Task<PaperClaimMessage> DeserializeAsync(byte[] data)
    {
        var (paper_Identity_Randomness, signature) = DeserializeTwoByteArrays(data);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(paper_Identity_Randomness, signature, pcPublicKey);

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
