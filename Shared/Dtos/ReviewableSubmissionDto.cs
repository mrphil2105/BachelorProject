namespace Apachi.Shared.Dtos;

public record ReviewableSubmissionDto(
    Guid SubmissionId,
    string Title,
    string Description,
    byte[] PaperSignature,
    byte[] EncryptedReviewRandomness,
    byte[] ReviewRandomnessSignature,
    DateTimeOffset CreatedDate
);
