using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Services;

public interface IMatchingService
{
    Task<List<MatchableSubmissionModel>> GetMatchableSubmissionsAsync();

    Task DownloadPaperAsync(byte[] paperHash, string paperFilePath);

    Task SendBidAsync(byte[] paperHash, bool wantsToReview);
}
