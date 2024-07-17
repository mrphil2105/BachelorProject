namespace Apachi.AvaloniaApp.Data;

public abstract class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public byte[] PasswordSalt { get; set; } = null!;

    public byte[] AuthenticationHash { get; set; } = null!;
}
