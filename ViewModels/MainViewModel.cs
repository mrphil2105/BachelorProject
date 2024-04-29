using Apachi.ViewModels.Services;

namespace Apachi.ViewModels;

public class MainViewModel : Conductor<Screen>
{
    private readonly ISessionService _sessionService;
    private readonly LoginViewModel _loginViewModel;
    private readonly RegisterViewModel _registerViewModel;
    private readonly MenuViewModel _menuViewModel;

    public MainViewModel(
        ISessionService sessionService,
        LoginViewModel loginViewModel,
        RegisterViewModel registerViewModel,
        MenuViewModel menuViewModel
    )
    {
        _sessionService = sessionService;
        _loginViewModel = loginViewModel;
        _registerViewModel = registerViewModel;
        _menuViewModel = menuViewModel;
    }

    public Task GoToLogin()
    {
        return ActivateItemAsync(_loginViewModel);
    }

    public Task GoToRegister()
    {
        return ActivateItemAsync(_registerViewModel);
    }

    public async Task UpdateLoginState()
    {
        if (!_sessionService.IsLoggedIn)
        {
            _menuViewModel.Reset();
            await GoToLogin();
            return;
        }

        _menuViewModel.DisplayUserPages(_sessionService.IsReviewer);
        await ActivateItemAsync(_menuViewModel);
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return ActivateItemAsync(_loginViewModel);
    }
}
