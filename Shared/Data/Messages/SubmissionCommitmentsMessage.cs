namespace Apachi.Shared.Data.Messages;

public record SubmissionCommitmentsMessage(
    byte[] SubmissionCommitment,
    byte[] IdentityCommitment,
    byte[] CommitmentsSignature, // Signed by K_S^-1
    byte[] SubmissionPublicKey
) : IMessage;
