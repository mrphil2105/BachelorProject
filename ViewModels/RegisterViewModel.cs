using Apachi.ViewModels.Services;
using Apachi.ViewModels.Validation;

namespace Apachi.ViewModels;

public class RegisterViewModel : Screen
{
    private readonly ISessionService _sessionService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _passwordConfirmation = string.Empty;
    private bool _isReviewer;
    private string _errorMessage = string.Empty;

    public RegisterViewModel(ISessionService sessionService)
    {
        _sessionService = sessionService;
        Validator = new ValidationAdapter<RegisterViewModel>(new RegisterViewModelValidator());
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

    public string PasswordConfirmation
    {
        get => _passwordConfirmation;
        set => Set(ref _passwordConfirmation, value);
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

    public Task Login()
    {
        return ((MainViewModel)Parent!).GoToLogin();
    }

    public async Task Register()
    {
        var isValid = await ValidateAsync();

        if (!isValid)
        {
            return;
        }

        ErrorMessage = string.Empty;
        var success = await _sessionService.RegisterAsync(Username, Password, IsReviewer);

        if (!success)
        {
            ErrorMessage = "A user with the specified username already exists.";
            return;
        }

        await _sessionService.LoginAsync(Username, Password, IsReviewer);
        await ((MainViewModel)Parent!).UpdateLoginState();
    }
}
