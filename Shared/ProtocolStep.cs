namespace Apachi.Shared;

public enum ProtocolStep
{
    SubmissionCreation = 1,
    SubmissionCommitmentsAndPublicKey = 2,
    SubmissionCommitmentSignature = 3,
    PaperShare = 4,
    Bid = 5,
    PaperReviewersMatching = 6,
    PaperAndReviewRandomnessShare = 7,
    Review = 8,
    ReviewCommitmentAndNonceSignature = 9,
    GroupKeyAndGradeRandomnessShare = 10,
    ReviewsShare = 11
}
