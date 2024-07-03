namespace Apachi.Shared.Dtos;

public record MatchableSubmissionDto(
    Guid SubmissionId,
    SubmissionStatus SubmissionStatus,
    string Title,
    string Description,
    byte[] PaperSignature,
    DateTimeOffset CreatedDate
);
