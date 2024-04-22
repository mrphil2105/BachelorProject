using System.Diagnostics.CodeAnalysis;

namespace Apachi.ViewModels.Auth
{
    public interface ISession
    {
        [MemberNotNullWhen(true, nameof(Username))]
        [MemberNotNullWhen(true, nameof(Role))]
        [MemberNotNullWhen(true, nameof(AesKey))]
        [MemberNotNullWhen(true, nameof(HmacKey))]
        bool IsLoggedIn { get; }

        string? Username { get; }

        UserRole? Role { get; }

        ReadOnlyMemory<byte>? AesKey { get; }

        ReadOnlyMemory<byte>? HmacKey { get; }

        Task<bool> LoginAsync(string username, string password);

        Task<bool> RegisterAsync(string username, string password, UserRole role);

        void Logout();
    }
}
