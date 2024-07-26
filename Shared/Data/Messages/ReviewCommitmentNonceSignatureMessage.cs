namespace Apachi.Shared.Data.Messages;

public record ReviewCommitmentNonceSignatureMessage(
    byte[] Signature // Signed by K_R^-1
) : IMessage;
