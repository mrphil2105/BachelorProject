namespace Apachi.AvaloniaApp.Data;

public class Submission
{
    public Guid Id { get; set; }

    public byte[] EncryptedSecrets { get; set; } = null!;

    public byte[] SecretsHmac { get; set; } = null!;

    public byte[] SubmissionCommitmentSignature { get; set; } = null!;

    public User User { get; set; } = null!;
}
