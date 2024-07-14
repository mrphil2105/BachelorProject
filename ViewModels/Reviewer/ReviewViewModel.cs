using Apachi.Shared.Dtos;

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

    public bool IsReviewer => true;

    public Task GoToList()
    {
        return ActivateItemAsync(_listViewModel);
    }

    public Task GoToAssessment(ReviewableSubmissionDto reviewableSubmissionDto)
    {
        _assessmentViewModel.ReviewableSubmissionDto = reviewableSubmissionDto;
        return ActivateItemAsync(_assessmentViewModel);
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return ActivateItemAsync(_listViewModel);
    }
}
