namespace Apachi.Shared;

public enum ReviewStatus
{
    Open, // Reviewer has yet to respond.
    Pass, // Reviewer has chosen to not review.
    Closed, // Reviewer did not respond in time.

    Pending, // Reviewer has yet to review.
    Accepted, // Reviewer has accepted paper.
    Rejected // Reviewer has rejected paper.
}
