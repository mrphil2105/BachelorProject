using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;

namespace Apachi.ViewModels.Reviewer;

public class ReviewableSubmissionsViewModel
    : Conductor<ReviewableSubmissionDto>.Collection.AllActive,
        IMenuPageViewModel
{
    private readonly IViewService _viewService;
    private readonly IReviewService _reviewService;
    private readonly IReviewerService _reviewerService;
    private bool _isLoading;

    public ReviewableSubmissionsViewModel(
        IViewService viewService,
        IReviewService reviewService,
        IReviewerService reviewerService
    )
    {
        _viewService = viewService;
        _reviewService = reviewService;
        _reviewerService = reviewerService;
    }

    public string PageName => "Review";

    public bool IsReviewer => true;

    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    public async Task DownloadPaper(ReviewableSubmissionDto reviewableSubmissionDto)
    {
        var paperFilePath = await _viewService.ShowSaveFileDialogAsync(this);

        if (paperFilePath == null)
        {
            return;
        }

        await _reviewService.DownloadPaperAsync(
            reviewableSubmissionDto.SubmissionId,
            reviewableSubmissionDto.PaperSignature,
            paperFilePath
        );
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        List<ReviewableSubmissionDto> reviewableSubmissionDtos;

        try
        {
            IsLoading = true;
            reviewableSubmissionDtos = await _reviewerService.GetReviewableSubmissionsAsync();
        }
        catch (HttpRequestException exception)
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

        Items.Clear();
        Items.AddRange(reviewableSubmissionDtos);
        IsLoading = false;
    }
}
