using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;

namespace Apachi.ViewModels.Reviewer;

public class OpenSubmissionsViewModel : Conductor<OpenSubmissionDto>.Collection.AllActive, IMenuPageViewModel
{
    private readonly IViewService _viewService;
    private readonly IReviewService _reviewService;
    private bool _isLoading;

    public OpenSubmissionsViewModel(IViewService viewService, IReviewService reviewService)
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

    public async Task DownloadPaper(OpenSubmissionDto openSubmissionDto)
    {
        var paperFilePath = await _viewService.ShowSaveFileDialogAsync(this);

        if (paperFilePath == null)
        {
            return;
        }

        await _reviewService.DownloadPaperAsync(
            openSubmissionDto.SubmissionId,
            openSubmissionDto.PaperSignature,
            paperFilePath
        );
    }

    public Task BidReview(OpenSubmissionDto openSubmissionDto)
    {
        return SendBidAsync(openSubmissionDto, true);
    }

    public Task BidAbstain(OpenSubmissionDto openSubmissionDto)
    {
        return SendBidAsync(openSubmissionDto, false);
    }

    private async Task SendBidAsync(OpenSubmissionDto openSubmissionDto, bool wantsToReview)
    {
        try
        {
            await _reviewService.SendBidAsync(openSubmissionDto.SubmissionId, wantsToReview);
        }
        catch (HttpRequestException exception)
        {
            await _viewService.ShowMessageBoxAsync(
                this,
                $"Unable to send bid: {exception.Message}",
                "Bid Failure",
                kind: MessageBoxKind.Error
            );
        }
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        List<OpenSubmissionDto> openSubmissionDtos;

        try
        {
            IsLoading = true;
            openSubmissionDtos = await _reviewService.GetOpenSubmissionsAsync();
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
        Items.AddRange(openSubmissionDtos);
        IsLoading = false;
    }
}
