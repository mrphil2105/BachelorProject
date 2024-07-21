namespace Apachi.Shared.Data;

public class Reviewer
{
    public Guid Id { get; set; }

    public required byte[] PublicKey { get; set; } // K_R

    public required byte[] EncryptedSharedKey { get; set; } // Encrypted with K_PC

    public required byte[] SharedKeySignature { get; set; } // Signed by K_R^-1
}
