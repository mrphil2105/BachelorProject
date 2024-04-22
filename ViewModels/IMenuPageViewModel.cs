using Apachi.ViewModels.Auth;

namespace Apachi.ViewModels;

public interface IMenuPageViewModel
{
    string PageName { get; }

    UserRole Role { get; }

    void Reset();
}
