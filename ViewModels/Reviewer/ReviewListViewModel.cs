using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;

namespace Apachi.ViewModels.Reviewer;

public class ReviewListViewModel : Conductor<ReviewableSubmissionModel>.Collection.AllActive
{
    private readonly IViewService _viewService;
    private readonly IReviewService _reviewService;
    private bool _isLoading;

    public ReviewListViewModel(IViewService viewService, IReviewService reviewService)
    {
        _viewService = viewService;
        _reviewService = reviewService;
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    public async Task DownloadPaper(ReviewableSubmissionModel model)
    {
        var paperFilePath = await _viewService.ShowSaveFileDialogAsync(this);

        if (paperFilePath == null)
        {
            return;
        }

        await _reviewService.DownloadPaperAsync(model.LogEntryId, paperFilePath);
    }

    public Task Review(ReviewableSubmissionModel model)
    {
        return ((ReviewViewModel)Parent!).GoToReview(model);
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        List<ReviewableSubmissionModel> models;

        try
        {
            IsLoading = true;
            models = await _reviewService.GetReviewableSubmissionsAsync();
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
