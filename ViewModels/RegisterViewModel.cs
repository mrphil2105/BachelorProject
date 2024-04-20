using Apachi.ViewModels.Auth;
using Apachi.ViewModels.Validation;

namespace Apachi.ViewModels;

public class RegisterViewModel : PageViewModel
{
    private readonly ISession _session;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _passwordConfirmation = string.Empty;
    private bool _isReviewer;
    private string _errorMessage = string.Empty;

    public RegisterViewModel(ISession session)
    {
        _session = session;
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
        return Parent!.GoToLogin();
    }

    public async Task Register()
    {
        var isValid = await ValidateAsync();

        if (!isValid)
        {
            return;
        }

        ErrorMessage = string.Empty;
        var success = await _session.RegisterAsync(
            Username,
            Password,
            IsReviewer ? UserRole.Reviewer : UserRole.Submitter
        );

        if (!success)
        {
            ErrorMessage = "A user with the specified username already exists.";
            return;
        }

        await _session.LoginAsync(Username, Password);
        await Parent!.UpdateLoginState();
    }
}
