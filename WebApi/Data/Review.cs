using Apachi.Shared;

namespace Apachi.WebApi.Data;

public class Review
{
    public Guid Id { get; set; }

    public ReviewStatus Status { get; set; }

    public byte[]? EncryptedReviewRandomness { get; set; } // Encrypted with K_PCR

    public string? Assessment { get; set; }

    public byte[]? AssessmentSignature { get; set; } // Signed by K_R^-1

    public byte[]? ReviewCommitmentSignature { get; set; } // Signed by K_R^-1

    public byte[]? ReviewNonceSignature { get; set; } // Signed by K_R^-1

    public byte[]? EncryptedGroupKey { get; set; } // Encrypted with K_PCR

    public byte[]? GroupKeySignature { get; set; } // Signed by K_PC^-1

    public byte[]? EncryptedGradeRandomness { get; set; } // Encrypted with K_PCR

    public byte[]? GradeRandomnessSignature { get; set; } // Signed by K_PC^-1

    public Guid ReviewerId { get; set; }

    public Reviewer Reviewer { get; set; } = null!;

    public Guid SubmissionId { get; set; }

    public Submission Submission { get; set; } = null!;
}
