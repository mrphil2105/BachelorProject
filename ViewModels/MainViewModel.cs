using Apachi.ViewModels.Auth;

namespace Apachi.ViewModels;

public class MainViewModel : Conductor<Screen>
{
    private readonly ISession _session;
    private readonly LoginViewModel _loginViewModel;
    private readonly RegisterViewModel _registerViewModel;
    private readonly MenuViewModel _menuViewModel;

    public MainViewModel(
        ISession session,
        LoginViewModel loginViewModel,
        RegisterViewModel registerViewModel,
        MenuViewModel menuViewModel
    )
    {
        _session = session;
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
        if (!_session.IsLoggedIn)
        {
            _menuViewModel.Reset();
            await GoToLogin();
            return;
        }

        _menuViewModel.DisplayUserPages(_session.Role.Value);
        await ActivateItemAsync(_menuViewModel);
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return ActivateItemAsync(_loginViewModel);
    }
}
