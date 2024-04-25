using Apachi.ViewModels.Auth;

namespace Apachi.AvaloniaApp.Data;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public byte[] PasswordSalt { get; set; } = null!;

    public byte[] AuthenticationHash { get; set; } = null!;

    public UserRole Role { get; set; }

    public List<Submission> Submissions { get; set; } = new List<Submission>();
}
