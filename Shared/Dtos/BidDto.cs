namespace Apachi.Shared.Dtos;

public record BidDto(Guid SubmissionId, Guid ReviewerId, bool WantsToReview);
