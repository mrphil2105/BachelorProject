using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Reviewer;

public class DiscussViewModel : Conductor<Screen>, IMenuPageViewModel
{
    private readonly DiscussListViewModel _listViewModel;
    private readonly DiscussReviewsViewModel _reviewsViewModel;
    private readonly DiscussMessagesViewModel _messagesViewModel;

    public DiscussViewModel(
        DiscussListViewModel listViewModel,
        DiscussReviewsViewModel reviewsViewModel,
        DiscussMessagesViewModel messagesViewModel
    )
    {
        _listViewModel = listViewModel;
        _reviewsViewModel = reviewsViewModel;
        _messagesViewModel = messagesViewModel;
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
        _reviewsViewModel.Items.Clear();
        _reviewsViewModel.Items.AddRange(model.Reviews);
        return ActivateItemAsync(_reviewsViewModel);
    }

    public Task GoToMessages(DiscussableSubmissionModel model)
    {
        _messagesViewModel.Model = model;
        return ActivateItemAsync(_messagesViewModel);
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        await ActivateItemAsync(_listViewModel);
        await base.OnActivateAsync(cancellationToken);
    }
}
