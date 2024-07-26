using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Services;

public interface IReviewService
{
    Task<List<MatchableSubmissionModel>> GetMatchableSubmissionsAsync();

    Task<List<ReviewableSubmissionModel>> GetReviewableSubmissionsAsync();

    Task DownloadPaperAsync(Guid logEntryId, string paperFilePath);

    Task SendBidAsync(Guid logEntryId, bool wantsToReview);

    Task SendReviewAsync(Guid logEntryId, string review);
}
