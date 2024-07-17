namespace Apachi.AvaloniaApp.Data;

public class Submission
{
    public Guid Id { get; set; }

    public byte[] EncryptedPrivateKey { get; set; } = null!; // K_S^-1

    public byte[] EncryptedIdentityRandomness { get; set; } = null!; // r_i

    public byte[] SubmissionCommitmentSignature { get; set; } = null!; // Signed by K_PC^-1

    public Guid SubmitterId { get; set; }

    public Submitter Submitter { get; set; } = null!;
}
