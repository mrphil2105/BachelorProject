namespace Apachi.Shared;

public enum ReviewStatus
{
    Matching, // Reviewer has yet to respond.
    Abstain, // Reviewer has chosen to not review.
    Pending, // Reviewer has yet to review.

    Accepted, // Reviewer has accepted paper.
    Rejected // Reviewer has rejected paper.
}
