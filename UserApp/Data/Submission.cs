namespace Apachi.UserApp.Data;

public class Submission
{
    public Guid Id { get; set; }

    public required byte[] EncryptedPrivateKey { get; set; } // K_S^-1

    public required byte[] EncryptedIdentityRandomness { get; set; } // r_i

    public required Guid SubmitterId { get; set; }

    public Submitter Submitter { get; set; } = null!;
}
