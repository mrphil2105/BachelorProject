namespace Apachi.AvaloniaApp.Data;

public class Reviewer : User
{
    public byte[] EncryptedPrivateKey { get; set; } = null!; // K_R^-1

    public byte[] EncryptedSharedKey { get; set; } = null!; // Shared key between PC and R.
}
