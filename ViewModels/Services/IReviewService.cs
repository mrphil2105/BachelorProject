using Apachi.Shared.Dtos;

namespace Apachi.ViewModels.Services;

public interface IReviewService
{
    Task<List<OpenSubmissionDto>> GetOpenSubmissionsAsync();

    Task DownloadPaperAsync(Guid submissionId, byte[] paperSignature, string paperFilePath);

    Task SendBidAsync(Guid submissionId, bool wantsToReview);
}
