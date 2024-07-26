namespace Apachi.Shared.Data.Messages;

public record ReviewMessage(
    byte[] EncryptedReview, // Encrypted with K_PCR
    byte[] ReviewSignature // Signed by K_R^-1
) : IMessage;
