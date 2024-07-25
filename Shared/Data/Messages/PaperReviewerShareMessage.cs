namespace Apachi.Shared.Data.Messages;

public record PaperReviewerShareMessage(
    byte[] EncryptedPaper, // Encrypted with K_PCR
    byte[] PaperSignature // Signed by K_PC^-1
) : IMessage;
