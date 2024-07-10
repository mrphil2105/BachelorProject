namespace Apachi.ViewModels.Services;

public interface ISubmissionService
{
    Task SubmitPaperAsync(string title, string description, string paperFilePath);
}
