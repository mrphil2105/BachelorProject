namespace Apachi.Shared.Dtos;

public record SubmissionToReviewDto(
    Guid SubmissionId,
    SubmissionStatus SubmissionStatus,
    byte[] PaperSignature,
    DateTimeOffset CreatedDate
);
