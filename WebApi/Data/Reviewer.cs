namespace Apachi.WebApi.Data;

public class Reviewer
{
    public Guid Id { get; set; }

    public byte[] ReviewerPublicKey { get; set; } = null!; // K_R

    public byte[] EncryptedSharedKey { get; set; } = null!; // Shared key between PC and R.

    public List<Review> Reviews { get; set; } = new List<Review>();
}
