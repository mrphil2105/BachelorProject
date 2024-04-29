namespace Apachi.AvaloniaApp.Data;

public class Submission
{
    public Guid Id { get; set; }

    public byte[] EncryptedPrivateKey { get; set; } = null!;

    public byte[] EncryptedIdentityRandomness { get; set; } = null!;

    public byte[] SubmissionCommitmentSignature { get; set; } = null!;

    public Guid SubmitterId { get; set; }

    public Submitter Submitter { get; set; } = null!;
}
