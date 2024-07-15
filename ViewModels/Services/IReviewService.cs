using Apachi.Shared.Dtos;

namespace Apachi.ViewModels.Services;

public interface IReviewService
{
    Task<List<MatchableSubmissionDto>> GetMatchableSubmissionsAsync();

    Task DownloadPaperAsync(Guid submissionId, byte[] paperSignature, string paperFilePath);

    Task SendBidAsync(Guid submissionId, bool wantsToReview);

    Task SendAssessmentAsync(ReviewableSubmissionDto reviewableSubmissionDto, string assessment);
}
