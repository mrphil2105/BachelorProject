using Apachi.ViewModels.Services;
using Apachi.ViewModels.Validation;

namespace Apachi.ViewModels.Submitter;

public class SubmitViewModel : Screen, IMenuPageViewModel
{
    private readonly IViewService _viewService;
    private readonly ISubmissionService _submissionService;

    private string _paperFilePath = string.Empty;

    public SubmitViewModel(IViewService viewService, ISubmissionService submissionService)
    {
        _viewService = viewService;
        _submissionService = submissionService;
        Validator = new ValidationAdapter<SubmitViewModel>(new SubmitViewModelValidator());
    }

    public string PageName => "Submit";

    public bool IsReviewer => false;

    public string PaperFilePath
    {
        get => _paperFilePath;
        set => Set(ref _paperFilePath, value);
    }

    public async Task BrowseFile()
    {
        var filePaths = await _viewService.ShowOpenFileDialogAsync(this);

        if (filePaths != null && filePaths.Count > 0)
        {
            PaperFilePath = filePaths[0];
        }
    }

    public async Task SubmitPaper()
    {
        var isValid = await ValidateAsync();

        if (!isValid)
        {
            return;
        }

        try
        {
            await _submissionService.SubmitPaperAsync(PaperFilePath);
            await _viewService.ShowMessageBoxAsync(
                this,
                "The paper has been successfully submitted!",
                "Submission Successful",
                kind: MessageBoxKind.Information
            );
        }
        catch (Exception exception)
        {
            await _viewService.ShowMessageBoxAsync(
                this,
                $"Unable to submit submission: {exception.Message}",
                "Submission Failure",
                kind: MessageBoxKind.Error
            );
        }
    }
}
