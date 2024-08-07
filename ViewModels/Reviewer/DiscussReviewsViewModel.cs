using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Reviewer;

public class DiscussReviewsViewModel : Conductor<ReviewModel>.Collection.AllActive
{
    private DiscussableSubmissionModel? _model;

    public DiscussableSubmissionModel? Model
    {
        get => _model;
        set => Set(ref _model, value);
    }

    public Task Back()
    {
        return ((DiscussViewModel)Parent!).GoToList();
    }
}
