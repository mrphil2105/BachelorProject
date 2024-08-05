using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;

namespace Apachi.ViewModels.Reviewer;

public class MatchViewModel : Conductor<MatchableSubmissionModel>.Collection.AllActive, IMenuPageViewModel
{
    private readonly IViewService _viewService;
    private readonly IMatchingService _matchingService;
    private bool _isLoading;

    public MatchViewModel(IViewService viewService, IMatchingService matchingService)
    {
        _viewService = viewService;
        _matchingService = matchingService;
    }

    public string PageName => "Match";

    public int PageNumber => 1;

    public bool IsReviewer => true;

    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    public async Task DownloadPaper(MatchableSubmissionModel model)
    {
        var paperFilePath = await _viewService.ShowSaveFileDialogAsync(this);

        if (paperFilePath == null)
        {
            return;
        }

        await _matchingService.DownloadPaperAsync(model.PaperHash, paperFilePath);
    }

    public Task BidReview(MatchableSubmissionModel model)
    {
        return SendBidAsync(model, true);
    }

    public Task BidAbstain(MatchableSubmissionModel model)
    {
        return SendBidAsync(model, false);
    }

    private async Task SendBidAsync(MatchableSubmissionModel model, bool wantsToReview)
    {
        try
        {
            await _matchingService.SendBidAsync(model.PaperHash, wantsToReview);
            Items.Remove(model);
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
        List<MatchableSubmissionModel> models;

        try
        {
            IsLoading = true;
            models = await _matchingService.GetMatchableSubmissionsAsync();
        }
        catch (HttpRequestException exception)
        {
            await _viewService.ShowMessageBoxAsync(
                $"Unable to retrieve submissions: {exception.Message}",
                "Retrieval Failure",
                kind: MessageBoxKind.Error
            );
            IsLoading = false;
            return;
        }

        Items.Clear();
        Items.AddRange(models);
        IsLoading = false;
    }
}
