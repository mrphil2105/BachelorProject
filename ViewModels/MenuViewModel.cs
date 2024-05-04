namespace Apachi.ViewModels;

public class MenuViewModel : Conductor<IMenuPageViewModel>.Collection.OneActive
{
    private readonly Func<IEnumerable<IMenuPageViewModel>> _pageViewModelsFactory;

    public MenuViewModel(Func<IEnumerable<IMenuPageViewModel>> pageViewModelsFactory)
    {
        _pageViewModelsFactory = pageViewModelsFactory;
    }

    public Task GoToMenuPage(IMenuPageViewModel menuPage)
    {
        return ActivateItemAsync(menuPage);
    }

    public void DisplayUserPages(bool isReviewer)
    {
        Reset();
        var pageViewModels = _pageViewModelsFactory();
        var userPageViewModels = pageViewModels.Where(model => model.IsReviewer == isReviewer);
        Items.AddRange(userPageViewModels);
    }

    public void Reset()
    {
        foreach (var pageViewModel in Items)
        {
            if (pageViewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        Items.Clear();
    }
}
