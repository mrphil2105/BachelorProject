namespace Apachi.Shared.Dtos;

public record OpenSubmissionDto(
    Guid SubmissionId,
    SubmissionStatus SubmissionStatus,
    byte[] PaperSignature,
    DateTimeOffset CreatedDate
);
