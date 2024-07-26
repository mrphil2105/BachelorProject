namespace Apachi.Shared.Data;

public enum ProtocolStep
{
    Submission = 1,
    SubmissionIdentityCommitments = 2,
    SubmissionCommitmentSignature = 3,
    PaperReviewerShare = 4,
    Bid = 5,
    PaperReviewersMatching = 6,
    ReviewRandomnessReviewerShare = 7
}
