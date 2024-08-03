using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Reviewer;

public class ReviewViewModel : Conductor<Screen>, IMenuPageViewModel
{
    private readonly ReviewListViewModel _listViewModel;
    private readonly ReviewAssessmentViewModel _assessmentViewModel;

    public ReviewViewModel(ReviewListViewModel listViewModel, ReviewAssessmentViewModel assessmentViewModel)
    {
        _listViewModel = listViewModel;
        _assessmentViewModel = assessmentViewModel;
    }

    public string PageName => "Review";

    public int PageNumber => 2;

    public bool IsReviewer => true;

    public Task GoToList()
    {
        return ActivateItemAsync(_listViewModel);
    }

    public Task GoToReview(ReviewableSubmissionModel model)
    {
        _assessmentViewModel.Model = model;
        return ActivateItemAsync(_assessmentViewModel);
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        await ActivateItemAsync(_listViewModel);
        await base.OnActivateAsync(cancellationToken);
    }
}
