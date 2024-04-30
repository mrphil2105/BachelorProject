namespace Apachi.WebApi.Data;

public class Submission
{
    public Guid Id { get; set; }

    public byte[] SubmissionRandomness { get; set; } = null!;

    public byte[] ReviewRandomness { get; set; } = null!;

    public byte[] SubmissionCommitment { get; set; } = null!;

    public byte[] IdentityCommitment { get; set; } = null!;

    public byte[] SubmissionPublicKey { get; set; } = null!;

    public byte[] SubmissionSignature { get; set; } = null!;

    public List<Review> Reviews { get; set; } = new List<Review>();
}
