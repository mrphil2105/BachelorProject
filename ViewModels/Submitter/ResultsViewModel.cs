using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Submitter;

public class ResultsViewModel : Conductor<Screen>, IMenuPageViewModel
{
    private readonly ResultsListViewModel _listViewModel;
    private readonly ReviewsViewModel _reviewsViewModel;

    public ResultsViewModel(ResultsListViewModel listViewModel, ReviewsViewModel reviewsViewModel)
    {
        _listViewModel = listViewModel;
        _reviewsViewModel = reviewsViewModel;
    }

    public string PageName => "Results";

    public int PageNumber => 2;

    public bool IsReviewer => false;

    public Task GoToList()
    {
        return ActivateItemAsync(_listViewModel);
    }

    public Task GoToReviews(GradedSubmissionModel model)
    {
        _reviewsViewModel.Items.Clear();
        _reviewsViewModel.Items.AddRange(model.Reviews);
        return ActivateItemAsync(_reviewsViewModel);
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        await ActivateItemAsync(_listViewModel);
        await base.OnActivateAsync(cancellationToken);
    }
}
