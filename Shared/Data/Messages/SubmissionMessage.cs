namespace Apachi.Shared.Data.Messages;

public record SubmissionMessage(
    byte[] EncryptedPaper, // Encrypted with K_PCS
    byte[] EncryptedSubmissionRandomness, // Encrypted with K_PCS
    byte[] EncryptedReviewRandomness, // Encrypted with K_PCS
    byte[] EncryptedSubmissionKey, // Encrypted with K_PC
    byte[] SubmissionSignature // Signed by K_S^-1
) : IMessage;
