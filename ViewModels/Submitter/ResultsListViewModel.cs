using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;

namespace Apachi.ViewModels.Submitter;

public class ResultsListViewModel : Conductor<GradedSubmissionModel>.Collection.AllActive
{
    private readonly IViewService _viewService;
    private readonly IDecisionService _decisionService;
    private bool _isLoading;

    public ResultsListViewModel(IViewService viewService, IDecisionService decisionService)
    {
        _viewService = viewService;
        _decisionService = decisionService;
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    public async Task DownloadPaper(GradedSubmissionModel model)
    {
        var paperFilePath = await _viewService.ShowSaveFileDialogAsync(this);

        if (paperFilePath == null)
        {
            return;
        }

        try
        {
            await _decisionService.DownloadPaperAsync(model.SubmissionId, paperFilePath);
        }
        catch (Exception exception)
        {
            await _viewService.ShowMessageBoxAsync(
                this,
                $"Unable to download paper: {exception.Message}",
                "Download Failure",
                kind: MessageBoxKind.Error
            );
        }
    }

    public Task Reviews(GradedSubmissionModel model)
    {
        return ((ResultsViewModel)Parent!).GoToReviews(model);
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsLoading = true;
            var models = await _decisionService.GetGradedSubmissionsAsync();
            Items.Clear();
            Items.AddRange(models);
        }
        catch (Exception exception)
        {
            await _viewService.ShowMessageBoxAsync(
                $"Unable to retrieve submissions: {exception.Message}",
                "Retrieval Failure",
                kind: MessageBoxKind.Error
            );
        }
        finally
        {
            IsLoading = false;
        }
    }
}
