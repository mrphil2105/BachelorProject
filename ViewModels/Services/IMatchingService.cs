using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Services;

public interface IMatchingService
{
    Task<List<MatchableSubmissionModel>> GetMatchableSubmissionsAsync();

    Task DownloadPaperAsync(Guid logEntryId, string paperFilePath);

    Task SendBidAsync(Guid logEntryId, bool wantsToReview);
}
