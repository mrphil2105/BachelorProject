using Apachi.ViewModels.Services;

namespace Apachi.ViewModels;

public class MenuViewModel : Conductor<IMenuPageViewModel>.Collection.OneActive
{
    private readonly ISessionService _sessionService;
    private readonly Func<IEnumerable<IMenuPageViewModel>> _pageViewModelsFactory;

    public MenuViewModel(ISessionService sessionService, Func<IEnumerable<IMenuPageViewModel>> pageViewModelsFactory)
    {
        _sessionService = sessionService;
        _pageViewModelsFactory = pageViewModelsFactory;
    }

    public Task GoToMenuPage(IMenuPageViewModel menuPage)
    {
        return ActivateItemAsync(menuPage);
    }

    public Task Logout()
    {
        _sessionService.Logout();
        return ((MainViewModel)Parent!).UpdateLoginState();
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
