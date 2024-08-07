using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Reviewer;

public class DiscussReviewsViewModel : Conductor<ReviewModel>.Collection.AllActive
{
    public Task Back()
    {
        return ((DiscussViewModel)Parent!).GoToList();
    }
}
