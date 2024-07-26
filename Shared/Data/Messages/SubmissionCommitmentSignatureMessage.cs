namespace Apachi.Shared.Data.Messages;

public record SubmissionCommitmentSignatureMessage(
    byte[] SubmissionCommitmentSignature // Signed by K_PC^-1
) : IMessage;
