using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Services;

public interface IDecisionService
{
    Task<List<GradedSubmissionModel>> GetGradedSubmissionsAsync();

    Task DownloadPaperAsync(Guid submissionId, string paperFilePath);
}
