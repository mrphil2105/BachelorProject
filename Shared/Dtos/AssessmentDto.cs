namespace Apachi.Shared.Dtos;

public record AssessmentDto(
    Guid ReviewerId,
    Guid SubmissionId,
    byte[] EncryptedAssessment,
    byte[] AssessmentSignature,
    byte[] ReviewCommitmentSignature,
    byte[] ReviewNonceSignature
);
