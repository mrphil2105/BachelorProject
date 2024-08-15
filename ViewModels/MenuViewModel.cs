using Apachi.ViewModels.Services;

namespace Apachi.ViewModels;

public class MenuViewModel : Conductor<IMenuPageViewModel>.Collection.OneActive
{
    private readonly ISessionService _sessionService;
    private readonly IClaimService _claimService;
    private readonly Func<IEnumerable<IMenuPageViewModel>> _pageViewModelsFactory;

    public MenuViewModel(
        ISessionService sessionService,
        IClaimService claimService,
        Func<IEnumerable<IMenuPageViewModel>> pageViewModelsFactory
    )
    {
        _sessionService = sessionService;
        _claimService = claimService;
        _pageViewModelsFactory = pageViewModelsFactory;
    }

    public async Task GoToMenuPage(IMenuPageViewModel menuPage)
    {
        if (!await CanCloseActivePage())
        {
            return;
        }

        await ActivateItemAsync(menuPage);
    }

    public async Task Logout()
    {
        if (!await CanCloseActivePage())
        {
            return;
        }

        _sessionService.Logout();
        await ((MainViewModel)Parent!).UpdateLoginState();
    }

    public void DisplayUserPages(bool isReviewer)
    {
        Reset();
        var pageViewModels = _pageViewModelsFactory();
        var userPageViewModels = pageViewModels
            .Where(model => model.IsReviewer == isReviewer)
            .OrderBy(model => model.PageNumber);
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

    private async Task<bool> CanCloseActivePage()
    {
        if (ActiveItem is IGuardClose guardClose)
        {
            if (!await guardClose.CanCloseAsync())
            {
                return false;
            }
        }

        return true;
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_sessionService.IsReviewer)
        {
            return _claimService.ClaimAcceptedPapersAsync();
        }

        return Task.CompletedTask;
    }
}
