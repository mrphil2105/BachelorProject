namespace Apachi.WebApi.Data;

public class Reviewer
{
    public Guid Id { get; set; }

    public byte[] ReviewerPublicKey { get; set; } = null!;

    public byte[] EncryptedSharedKey { get; set; } = null!;
}
