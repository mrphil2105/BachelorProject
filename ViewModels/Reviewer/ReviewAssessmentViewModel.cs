using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;
using Apachi.ViewModels.Validation;

namespace Apachi.ViewModels.Reviewer;

public class ReviewAssessmentViewModel : Screen
{
    private readonly IViewService _viewService;
    private readonly IReviewService _reviewService;

    private ReviewableSubmissionModel? _reviewableSubmissionDto;
    private string _review = string.Empty;
    private bool _isDirty;
    private bool _hasSubmitted;

    public ReviewAssessmentViewModel(IViewService viewService, IReviewService reviewService)
    {
        _viewService = viewService;
        _reviewService = reviewService;
        Validator = new ValidationAdapter<ReviewAssessmentViewModel>(new ReviewAssessmentViewModelValidator());
    }

    public ReviewableSubmissionModel? ReviewableSubmissionModel
    {
        get => _reviewableSubmissionDto;
        set => Set(ref _reviewableSubmissionDto, value);
    }

    public string Review
    {
        get => _review;
        set
        {
            Set(ref _review, value);
            IsDirty = true;
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set => Set(ref _isDirty, value);
    }

    public bool HasSubmitted
    {
        get => _hasSubmitted;
        set => Set(ref _hasSubmitted, value);
    }

    public async Task SubmitReview()
    {
        var isValid = await ValidateAsync();

        if (!isValid)
        {
            return;
        }

        try
        {
            await _reviewService.SendReviewAsync(ReviewableSubmissionModel!.LogEntryId, Review);
            IsDirty = false;
            HasSubmitted = true;
            await _viewService.ShowMessageBoxAsync(
                this,
                "The review has been successfully sent!",
                "Review Successful",
                kind: MessageBoxKind.Information
            );
        }
        catch (HttpRequestException exception)
        {
            await _viewService.ShowMessageBoxAsync(
                this,
                $"Unable to send review: {exception.Message}",
                "Review Failure",
                kind: MessageBoxKind.Error
            );
        }
    }

    public Task Back()
    {
        return ((ReviewViewModel)Parent!).GoToList();
    }

    public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
    {
        if (!IsActive || !IsDirty)
        {
            return true;
        }

        var result = await _viewService.ShowMessageBoxAsync(
            this,
            "You have not submitted your review. Are you sure you want to close?",
            "Unsubmitted Review",
            MessageBoxButton.YesNo,
            MessageBoxKind.Question
        );
        return result == MessageBoxResult.Yes;
    }
}
