namespace Apachi.Shared.Messages;

// 18: {|P;r_s|}K_PC^-1;NIZK{g in G_a}
public class PaperRevealMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] SubmissionRandomness { get; init; }

    public required byte[] MembershipProof { get; init; }

    public async Task<byte[]> SerializeAsync()
    {
        var paper_Randomness = SerializeByteArrays(Paper, SubmissionRandomness);

        var pcPrivateKey = GetPCPrivateKey();
        var signature = await CalculateSignatureAsync(paper_Randomness, pcPrivateKey);

        var serialized = SerializeByteArrays(paper_Randomness, signature, MembershipProof);
        return serialized;
    }

    public static async Task<PaperRevealMessage> DeserializeAsync(byte[] data)
    {
        var (paper_Randomness, signature, membershipProof) = DeserializeThreeByteArrays(data);

        var pcPublicKey = GetPCPublicKey();
        await ThrowIfInvalidSignatureAsync(paper_Randomness, signature, pcPublicKey);

        var (paper, submissionRandomness) = DeserializeTwoByteArrays(paper_Randomness);
        var message = new PaperRevealMessage
        {
            Paper = paper,
            SubmissionRandomness = submissionRandomness,
            MembershipProof = membershipProof
        };
        return message;
    }
}
