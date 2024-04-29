using System.Diagnostics.CodeAnalysis;

namespace Apachi.ViewModels.Services;

public interface ISessionService
{
    [MemberNotNullWhen(true, nameof(Username))]
    [MemberNotNullWhen(true, nameof(UserId))]
    [MemberNotNullWhen(true, nameof(AesKey))]
    [MemberNotNullWhen(true, nameof(HmacKey))]
    bool IsLoggedIn { get; }

    bool IsReviewer { get; }

    string? Username { get; }

    Guid? UserId { get; }

    byte[]? AesKey { get; }

    byte[]? HmacKey { get; }

    Task<bool> LoginAsync(string username, string password, bool isReviewer);

    Task<bool> RegisterAsync(string username, string password, bool isReviewer);

    void Logout();
}
