using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Services;

public interface IDiscussionService
{
    Task<List<DiscussableSubmissionModel>> GetDiscussableSubmissionsAsync();

    Task DownloadPaperAsync(byte[] paperHash, string paperFilePath);
}
