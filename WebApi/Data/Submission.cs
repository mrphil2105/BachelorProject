using Apachi.Shared;

namespace Apachi.WebApi.Data;

public class Submission
{
    public Guid Id { get; set; }

    public SubmissionStatus Status { get; set; }

    // Created by submitter.

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public byte[] SubmissionRandomness { get; set; } = null!; // r_s

    public byte[] ReviewRandomness { get; set; } = null!; // r_r

    public byte[] SubmissionCommitment { get; set; } = null!; // C(P, r_s)

    public byte[] IdentityCommitment { get; set; } = null!; // C(P, r_i)

    public byte[] SubmissionPublicKey { get; set; } = null!; // K_S

    public byte[] SubmissionSignature { get; set; } = null!; // Signed by K_S^-1

    public byte[] PaperSignature { get; set; } = null!; // Signed by K_PC^1

    // Created by program committee.

    public byte[] ReviewCommitment { get; set; } = null!; // C(P, r_r)

    public byte[] ReviewNonce { get; set; } = null!;

    public byte[]? MatchingSignature { get; set; }

    // Timestamps.

    public DateTimeOffset CreatedDate { get; set; }

    public DateTimeOffset UpdatedDate { get; set; }

    public DateTimeOffset? ClosedDate { get; set; }

    public List<Review> Reviews { get; set; } = new List<Review>();
}
