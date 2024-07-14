using Apachi.Shared.Dtos;
using Apachi.ViewModels.Validation;

namespace Apachi.ViewModels.Reviewer;

public class ReviewAssessmentViewModel : Screen
{
    private ReviewableSubmissionDto? _reviewableSubmissionDto;
    private string _assessment = string.Empty;
    private bool _isDirty;

    public ReviewAssessmentViewModel()
    {
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

        throw new NotImplementedException();
    }

    public Task Back()
    {
        return ((ReviewViewModel)Parent!).GoToList();
    }
}
