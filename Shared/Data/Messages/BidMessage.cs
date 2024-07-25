namespace Apachi.Shared.Data.Messages;

public record BidMessage(
    byte[] EncryptedPaper, // Encrypted with K_PCR
    byte[] EncryptedBid, // Encrypted with K_PCR
    byte[] Signature // Signed by K_R^-1
) : IMessage;
