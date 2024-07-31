namespace Apachi.Shared;

public enum ProtocolStep
{
    SubmissionCreation = 1,
    SubmissionCommitmentsAndPublicKey = 2,
    SubmissionCommitmentSignature = 3,
    PaperReviewerShare = 4,
    Bid = 5,
    PaperReviewersMatching = 6,
    PaperAndReviewRandomnessReviewerShare = 7,
    Review = 8,
    ReviewCommitmentAndNonceSignature = 9
}
