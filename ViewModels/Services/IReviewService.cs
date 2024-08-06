using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Services;

public interface IReviewService
{
    Task<List<ReviewableSubmissionModel>> GetReviewableSubmissionsAsync();

    Task DownloadPaperAsync(byte[] paperHash, string paperFilePath);

    Task SendReviewAsync(byte[] paperHash, string review);
}
