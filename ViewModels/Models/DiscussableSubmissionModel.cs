namespace Apachi.ViewModels.Models;

public class DiscussableSubmissionModel : PropertyChangedBase
{
    private GradeModel? _grade;

    public required byte[] PaperHash { get; init; }

    public required List<DiscussReviewModel> Reviews { get; init; }

    public GradeModel? Grade
    {
        get => _grade;
        set => Set(ref _grade, value);
    }
}
