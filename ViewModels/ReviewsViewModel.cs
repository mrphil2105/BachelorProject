using Apachi.ViewModels.Models;
using Apachi.ViewModels.Reviewer;
using Apachi.ViewModels.Submitter;

namespace Apachi.ViewModels;

public class ReviewsViewModel : Conductor<ReviewModel>.Collection.AllActive
{
    public Task Back()
    {
        if (Parent is DiscussViewModel discussViewModel)
        {
            return discussViewModel.GoToList();
        }

        return ((ResultsViewModel)Parent!).GoToList();
    }
}
