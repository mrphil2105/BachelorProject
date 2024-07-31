namespace Apachi.UserApp.Data;

public class Reviewer : User
{
    public required byte[] EncryptedPrivateKey { get; set; } // K_R^-1

    public required byte[] EncryptedSharedKey { get; set; } // Shared key between PC and R.
}
