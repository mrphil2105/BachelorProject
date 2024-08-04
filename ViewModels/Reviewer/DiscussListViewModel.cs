using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;

namespace Apachi.ViewModels.Reviewer;

public class DiscussListViewModel : Conductor<DiscussableSubmissionModel>.Collection.AllActive
{
    private readonly IViewService _viewService;
    private readonly IDiscussionService _discussionService;
    private bool _isLoading;

    public DiscussListViewModel(IViewService viewService, IDiscussionService discussionService)
    {
        _viewService = viewService;
        _discussionService = discussionService;
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    public async Task DownloadPaper(DiscussableSubmissionModel model)
    {
        var paperFilePath = await _viewService.ShowSaveFileDialogAsync(this);

        if (paperFilePath == null)
        {
            return;
        }

        try
        {
            await _discussionService.DownloadPaperAsync(model.PaperHash, paperFilePath);
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

    public Task Reviews(DiscussableSubmissionModel model)
    {
        return ((DiscussViewModel)Parent!).GoToReviews(model);
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsLoading = true;
            var models = await _discussionService.GetDiscussableSubmissionsAsync();
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
