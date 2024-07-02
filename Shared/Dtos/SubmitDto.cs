namespace Apachi.Shared.Dtos;

public record SubmitDto(
    string Title,
    string Description,
    byte[] EncryptedPaper,
    byte[] EncryptedSubmissionKey,
    byte[] SubmissionRandomness,
    byte[] ReviewRandomness,
    byte[] SubmissionCommitment,
    byte[] IdentityCommitment,
    byte[] SubmissionPublicKey,
    byte[] SubmissionSignature
);
