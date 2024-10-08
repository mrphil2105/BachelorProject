using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;
using Apachi.ViewModels.Validation;

namespace Apachi.ViewModels.Reviewer;

public class ReviewAssessmentViewModel : Screen
{
    private readonly IViewService _viewService;
    private readonly IReviewService _reviewService;

    private ReviewableSubmissionModel? _model;
    private string _review = string.Empty;
    private bool _isDirty;
    private bool _hasSubmitted;

    public ReviewAssessmentViewModel(IViewService viewService, IReviewService reviewService)
    {
        _viewService = viewService;
        _reviewService = reviewService;
        Validator = new ValidationAdapter<ReviewAssessmentViewModel>(new ReviewAssessmentViewModelValidator());
    }

    public ReviewableSubmissionModel? Model
    {
        get => _model;
        set => Set(ref _model, value);
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

    public async Task SendReview()
    {
        var isValid = await ValidateAsync();

        if (!isValid)
        {
            return;
        }

        try
        {
            await _reviewService.SendReviewAsync(Model!.PaperHash, Review);
            IsDirty = false;
            HasSubmitted = true;
            await _viewService.ShowMessageBoxAsync(
                this,
                "The review has been successfully sent!",
                "Review Successful",
                kind: MessageBoxKind.Information
            );
        }
        catch (Exception exception)
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

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        Review = string.Empty;
        IsDirty = false;
        HasSubmitted = false;
        return Task.CompletedTask;
    }
}
