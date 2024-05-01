namespace Apachi.WebApi.Data;

public class Submission
{
    public Guid Id { get; set; }

    public byte[] SubmissionRandomness { get; set; } = null!; // r_s

    public byte[] ReviewRandomness { get; set; } = null!; // r_r

    public byte[] SubmissionCommitment { get; set; } = null!; // C(P, r_s)

    public byte[] IdentityCommitment { get; set; } = null!; // C(P, r_i)

    public byte[] SubmissionPublicKey { get; set; } = null!; // K_S

    public byte[] SubmissionSignature { get; set; } = null!; // Signed by K_S^-1

    public byte[] PaperSignature { get; set; } = null!; // Signed by K_PC^1

    public DateTimeOffset CreatedDate { get; set; }

    public DateTimeOffset? ClosedDate { get; set; }

    public List<Review> Reviews { get; set; } = new List<Review>();
}
