using Apachi.ViewModels.Auth;

namespace Apachi.ViewModels;

public class MainViewModel : Conductor<Screen>
{
    private readonly ISession _session;
    private readonly LoginViewModel _loginViewModel;
    private readonly RegisterViewModel _registerViewModel;

    public MainViewModel(ISession session, LoginViewModel loginViewModel, RegisterViewModel registerViewModel)
    {
        _session = session;
        _loginViewModel = loginViewModel;
        _registerViewModel = registerViewModel;
    }

    public Task GoToLogin()
    {
        return ActivateItemAsync(_loginViewModel);
    }

    public Task GoToRegister()
    {
        return ActivateItemAsync(_registerViewModel);
    }

    public async Task UpdateLoginState() { }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return ActivateItemAsync(_loginViewModel);
    }
}
