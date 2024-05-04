using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;

namespace Apachi.ViewModels.Reviewer;

public class SubmissionListViewModel : Conductor<SubmissionToReviewModel>.Collection.AllActive, IMenuPageViewModel
{
    private readonly IViewService _viewService;
    private readonly IReviewService _reviewService;
    private bool _isLoading;

    public SubmissionListViewModel(IViewService viewService, IReviewService reviewService)
    {
        _viewService = viewService;
        _reviewService = reviewService;
    }

    public string PageName => "Submissions";

    public bool IsReviewer => true;

    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    public async Task DownloadPaper(SubmissionToReviewModel submissionToReviewModel)
    {
        var paperFilePath = await _viewService.ShowSaveFileDialogAsync(this);

        if (paperFilePath == null)
        {
            return;
        }

        await _reviewService.DownloadPaperAsync(submissionToReviewModel.Id, paperFilePath);
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        List<SubmissionToReviewDto> submissionToReviewDtos;

        try
        {
            IsLoading = true;
            submissionToReviewDtos = await _reviewService.GetSubmissionsToReviewAsync();
        }
        catch (Exception exception)
        {
            await _viewService.ShowMessageBoxAsync(
                this,
                $"Unable to retrieve submissions: {exception.Message}",
                "Retrieval Failure",
                kind: MessageBoxKind.Error
            );
            IsLoading = false;
            return;
        }

        var submissionToReviewModels = submissionToReviewDtos.Select(dto => new SubmissionToReviewModel(dto));
        Items.Clear();
        Items.AddRange(submissionToReviewModels);
        IsLoading = false;
    }

    public void Reset()
    {
        Items.Clear();
    }
}
