using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;

namespace Apachi.ViewModels.Reviewer;

public class OpenSubmissionsViewModel : Conductor<OpenSubmissionModel>.Collection.AllActive, IMenuPageViewModel
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

    public async Task DownloadPaper(OpenSubmissionModel openSubmissionModel)
    {
        var paperFilePath = await _viewService.ShowSaveFileDialogAsync(this);

        if (paperFilePath == null)
        {
            return;
        }

        await _reviewService.DownloadPaperAsync(
            openSubmissionModel.Id,
            openSubmissionModel.PaperSignature,
            paperFilePath
        );
    }

    public Task BidReview(OpenSubmissionModel openSubmissionModel)
    {
        return SendBidAsync(openSubmissionModel, true);
    }

    public Task BidAbstain(OpenSubmissionModel openSubmissionModel)
    {
        return SendBidAsync(openSubmissionModel, false);
    }

    private async Task SendBidAsync(OpenSubmissionModel openSubmissionModel, bool wantsToReview)
    {
        try
        {
            await _reviewService.SendBidAsync(openSubmissionModel.Id, wantsToReview);
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

        var openSubmissionModels = openSubmissionDtos.Select(dto => new OpenSubmissionModel(dto));
        Items.Clear();
        Items.AddRange(openSubmissionModels);
        IsLoading = false;
    }
}
