using Apachi.ViewModels.Services;

namespace Apachi.ViewModels;

public class LoginViewModel : Screen
{
    private readonly ISessionService _sessionService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _isReviewer;
    private string _errorMessage = string.Empty;

    public LoginViewModel(ISessionService sessionService)
    {
        _sessionService = sessionService;
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

    public bool IsReviewer
    {
        get => _isReviewer;
        set => Set(ref _isReviewer, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => Set(ref _errorMessage, value);
    }

    public async Task Login()
    {
        ErrorMessage = string.Empty;
        var success = await _sessionService.LoginAsync(Username, Password, IsReviewer);

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

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
#if DEBUG
        Username = "foo";
        Password = "bar";
        await Login();
#endif
    }
}
