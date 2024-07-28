namespace Apachi.Shared.Messages;

// 1: {|{|P;r_s;r_r|}K_PCS;{|K_PCS|}K_PC|}K_S^-1
public class SubmissionCreationMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] SubmissionRandomness { get; init; }

    public required byte[] ReviewRandomness { get; init; }

    public required byte[] SubmissionKey { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] submissionPrivateKey)
    {
        var paperAndRandomnesses = SerializeByteArrays(Paper, SubmissionRandomness, ReviewRandomness);
        var encryptedPaperAndRandomnesses = await SymmetricEncryptAsync(paperAndRandomnesses, SubmissionKey);

        var pcPublicKey = GetPCPublicKey();
        var encryptedSubmissionKey = await AsymmetricEncryptAsync(SubmissionKey, pcPublicKey);

        var bytesToSign = SerializeByteArrays(encryptedPaperAndRandomnesses, encryptedSubmissionKey);
        var signature = await CalculateSignatureAsync(bytesToSign, submissionPrivateKey);

        var serialized = SerializeByteArrays(bytesToSign, signature);
        return serialized;
    }

    public static async Task<SubmissionCreationMessage> DeserializeAsync(byte[] data, byte[] submissionPublicKey)
    {
        var (bytesToVerify, signature) = DeserializeTwoByteArrays(data);

        await ThrowIfInvalidSignatureAsync(bytesToVerify, signature, submissionPublicKey);
        var (encryptedPaperAndRandomnesses, encryptedSubmissionKey) = DeserializeTwoByteArrays(bytesToVerify);

        var pcPrivateKey = GetPCPrivateKey();
        var submissionKey = await AsymmetricDecryptAsync(encryptedSubmissionKey, pcPrivateKey);

        var paperAndRandomnesses = await SymmetricDecryptAsync(encryptedPaperAndRandomnesses, submissionKey);
        var (paper, submissionRandomness, reviewRandomness) = DeserializeThreeByteArrays(paperAndRandomnesses);

        var message = new SubmissionCreationMessage
        {
            Paper = paper,
            SubmissionRandomness = submissionRandomness,
            ReviewRandomness = reviewRandomness,
            SubmissionKey = submissionKey
        };
        return message;
    }
}
