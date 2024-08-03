using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Reviewer;

public class DiscussViewModel : Conductor<Screen>, IMenuPageViewModel
{
    private readonly DiscussListViewModel _listViewModel;
    private readonly DiscussReviewsViewModel _reviewsViewModel;

    public DiscussViewModel(DiscussListViewModel listViewModel, DiscussReviewsViewModel reviewsViewModel)
    {
        _listViewModel = listViewModel;
        _reviewsViewModel = reviewsViewModel;
    }

    public string PageName => "Discuss";

    public int PageNumber => 3;

    public bool IsReviewer => true;

    public Task GoToList()
    {
        return ActivateItemAsync(_listViewModel);
    }

    public Task GoToReviews(DiscussableSubmissionModel model)
    {
        _reviewsViewModel.Model = model;
        return ActivateItemAsync(_reviewsViewModel);
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        await ActivateItemAsync(_listViewModel);
        await base.OnActivateAsync(cancellationToken);
    }
}
