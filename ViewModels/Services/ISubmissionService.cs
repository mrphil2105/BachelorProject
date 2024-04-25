namespace Apachi.ViewModels.Services;

public interface ISubmissionService
{
    Task SubmitPaperAsync(string paperFilePath);
}
