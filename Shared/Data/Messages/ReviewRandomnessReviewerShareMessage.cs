namespace Apachi.Shared.Data.Messages;

public record ReviewRandomnessReviewerShareMessage(
    byte[] EncryptedPaper, // Encrypted with K_PCR
    byte[] EncryptedReviewRandomness, // Encrypted with K_PCR,
    byte[] Signature // Signed by K_PC^-1
) : IMessage;
