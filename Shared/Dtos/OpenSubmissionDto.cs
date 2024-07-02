namespace Apachi.Shared.Dtos;

public record OpenSubmissionDto(
    Guid SubmissionId,
    SubmissionStatus SubmissionStatus,
    string Title,
    string Description,
    byte[] PaperSignature,
    DateTimeOffset CreatedDate
);
