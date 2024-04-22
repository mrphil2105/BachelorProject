using Apachi.ViewModels.Auth;

namespace Apachi.ViewModels;

public class LoginViewModel : Screen
{
    private readonly ISession _session;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(ISession session)
    {
        _session = session;
    }

    public string Username
    {
        get => _username;
        set => Set(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => Set(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => Set(ref _errorMessage, value);
    }

    public async Task Login()
    {
        ErrorMessage = string.Empty;
        var success = await _session.LoginAsync(Username, Password);

        if (!success)
        {
            ErrorMessage = "Invalid username or password.";
            return;
        }

        await ((MainViewModel)Parent!).UpdateLoginState();
    }

    public Task Register()
    {
        return ((MainViewModel)Parent!).GoToRegister();
    }
}
