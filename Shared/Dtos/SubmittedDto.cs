namespace Apachi.Shared.Dtos;

public record SubmittedDto(Guid SubmissionId, byte[] SubmissionCommitmentSignature);
