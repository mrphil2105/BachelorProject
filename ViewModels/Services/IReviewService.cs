using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Services;

public interface IReviewService
{
    Task<List<ReviewableSubmissionModel>> GetReviewableSubmissionsAsync();

    Task DownloadPaperAsync(Guid logEntryId, string paperFilePath);

    Task SendReviewAsync(Guid logEntryId, string review);
}
