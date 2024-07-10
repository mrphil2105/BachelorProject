namespace Apachi.Shared.Dtos;

public record MatchableSubmissionDto(
    Guid SubmissionId,
    string Title,
    string Description,
    byte[] PaperSignature,
    DateTimeOffset CreatedDate
);
