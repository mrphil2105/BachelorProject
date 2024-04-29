namespace Apachi.ViewModels;

public class MenuViewModel : Conductor<IMenuPageViewModel>.Collection.OneActive
{
    private readonly List<IMenuPageViewModel> _pageViewModels;

    public MenuViewModel(IEnumerable<IMenuPageViewModel> pageViewModels)
    {
        _pageViewModels = new List<IMenuPageViewModel>(pageViewModels);
    }

    public Task GoToMenuPage(IMenuPageViewModel menuPage)
    {
        return ActivateItemAsync(menuPage);
    }

    public void DisplayUserPages(bool isReviewer)
    {
        Reset();
        var userPageViewModels = _pageViewModels.Where(model => model.IsReviewer == isReviewer);
        Items.AddRange(userPageViewModels);
    }

    public void Reset()
    {
        foreach (var pageViewModel in Items)
        {
            pageViewModel.Reset();
        }

        Items.Clear();
    }
}
