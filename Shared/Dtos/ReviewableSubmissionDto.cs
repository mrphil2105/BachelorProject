namespace Apachi.Shared.Dtos;

public record ReviewableSubmissionDto(
    Guid SubmissionId,
    ReviewStatus ReviewStatus,
    string Title,
    string Description,
    byte[] PaperSignature,
    byte[] EncryptedReviewRandomness,
    byte[] ReviewRandomnessSignature,
    byte[] ReviewCommitment,
    byte[] ReviewNonce,
    DateTimeOffset CreatedDate
);
