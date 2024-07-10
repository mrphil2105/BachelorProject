namespace Apachi.Shared.Dtos;

public record SubmitDto(
    string Title,
    string Description,
    byte[] EncryptedPaper,
    byte[] EncryptedSubmissionKey,
    byte[] EncryptedSubmissionRandomness,
    byte[] EncryptedReviewRandomness,
    byte[] SubmissionCommitment,
    byte[] IdentityCommitment,
    byte[] SubmissionPublicKey,
    byte[] SubmissionSignature
);
