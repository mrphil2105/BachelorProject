using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;
using Apachi.ViewModels.Validation;

namespace Apachi.ViewModels.Reviewer;

public class ReviewAssessmentViewModel : Screen
{
    private readonly IViewService _viewService;
    private readonly IReviewService _reviewService;

    private ReviewableSubmissionDto? _reviewableSubmissionDto;
    private string _assessment = string.Empty;
    private bool _isDirty;

    public ReviewAssessmentViewModel(IViewService viewService, IReviewService reviewService)
    {
        _viewService = viewService;
        _reviewService = reviewService;
        Validator = new ValidationAdapter<ReviewAssessmentViewModel>(new ReviewAssessmentViewModelValidator());
    }

    public ReviewableSubmissionDto? ReviewableSubmissionDto
    {
        get => _reviewableSubmissionDto;
        set => Set(ref _reviewableSubmissionDto, value);
    }

    public string Assessment
    {
        get => _assessment;
        set
        {
            Set(ref _assessment, value);
            IsDirty = true;
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set => Set(ref _isDirty, value);
    }

    public Task SaveAssessment()
    {
        IsDirty = false;
        throw new NotImplementedException();
    }

    public async Task SubmitAssessment()
    {
        var isValid = await ValidateAsync();

        if (!isValid)
        {
            return;
        }

        try
        {
            await _reviewService.SendAssessmentAsync(ReviewableSubmissionDto!, Assessment);
            await _viewService.ShowMessageBoxAsync(
                this,
                "The assessment has been successfully sent!",
                "Assessment Successful",
                kind: MessageBoxKind.Information
            );
        }
        catch (HttpRequestException exception)
        {
            await _viewService.ShowMessageBoxAsync(
                this,
                $"Unable to send assessment: {exception.Message}",
                "Assessment Failure",
                kind: MessageBoxKind.Error
            );
        }
    }

    public Task Back()
    {
        return ((ReviewViewModel)Parent!).GoToList();
    }
}
