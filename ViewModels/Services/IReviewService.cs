using Apachi.Shared.Dtos;
using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Services;

public interface IReviewService
{
    Task<List<MatchableSubmissionModel>> GetMatchableSubmissionsAsync();

    Task DownloadPaperAsync(Guid logEntryId, string paperFilePath);

    Task SendBidAsync(Guid logEntryId, bool wantsToReview);

    Task SendAssessmentAsync(ReviewableSubmissionDto reviewableSubmissionDto, string assessment);

    Task SaveAssessmentAsync(Guid submissionId, string assessment);

    Task<string?> LoadAssessmentAsync(Guid submissionId);
}
