namespace Apachi.AvaloniaApp.Data;

public class Reviewer
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public byte[] PasswordSalt { get; set; } = null!;

    public byte[] AuthenticationHash { get; set; } = null!;

    public byte[] EncryptedPrivateKey { get; set; } = null!; // K_R^-1

    public byte[] EncryptedSharedKey { get; set; } = null!; // Shared key between PC and R.
}
