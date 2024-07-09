using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;

namespace Apachi.ViewModels.Reviewer;

public class MatchableSubmissionsViewModel : Conductor<MatchableSubmissionDto>.Collection.AllActive, IMenuPageViewModel
{
    private readonly IViewService _viewService;
    private readonly IReviewService _reviewService;
    private bool _isLoading;

    public MatchableSubmissionsViewModel(IViewService viewService, IReviewService reviewService)
    {
        _viewService = viewService;
        _reviewService = reviewService;
    }

    public string PageName => "Match";

    public bool IsReviewer => true;

    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    public async Task DownloadPaper(MatchableSubmissionDto matchableSubmissionDto)
    {
        var paperFilePath = await _viewService.ShowSaveFileDialogAsync(this);

        if (paperFilePath == null)
        {
            return;
        }

        await _reviewService.DownloadPaperAsync(
            matchableSubmissionDto.SubmissionId,
            matchableSubmissionDto.PaperSignature,
            paperFilePath
        );
    }

    public Task BidReview(MatchableSubmissionDto matchableSubmissionDto)
    {
        return SendBidAsync(matchableSubmissionDto, true);
    }

    public Task BidAbstain(MatchableSubmissionDto matchableSubmissionDto)
    {
        return SendBidAsync(matchableSubmissionDto, false);
    }

    private async Task SendBidAsync(MatchableSubmissionDto matchableSubmissionDto, bool wantsToReview)
    {
        try
        {
            await _reviewService.SendBidAsync(matchableSubmissionDto.SubmissionId, wantsToReview);
            Items.Remove(matchableSubmissionDto);
            await _viewService.ShowMessageBoxAsync(
                this,
                "The bid has been successfully sent!",
                "Bid Successful",
                kind: MessageBoxKind.Information
            );
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
        List<MatchableSubmissionDto> matchableSubmissionDtos;

        try
        {
            IsLoading = true;
            matchableSubmissionDtos = await _reviewService.GetMatchableSubmissionsAsync();
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
        Items.AddRange(matchableSubmissionDtos);
        IsLoading = false;
    }
}
