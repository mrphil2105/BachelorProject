namespace Apachi.Shared;

public enum SubmissionStatus
{
    Open, // Submission has no reviewers yet.
    Pending, // Submission is being reviewed.
    Accepted, // Submission has been accepted.
    Rejected // Submission has been rejected.
}
