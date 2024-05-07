using Apachi.Shared.Dtos;

namespace Apachi.ViewModels.Services;

public interface IReviewService
{
    Task<List<SubmissionToReviewDto>> GetSubmissionsToReviewAsync();

    Task DownloadPaperAsync(Guid submissionId, string paperFilePath);

    Task SendBidAsync(Guid submissionId, bool wantsToReview);
}
