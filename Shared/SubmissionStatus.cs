namespace Apachi.Shared;

public enum SubmissionStatus
{
    Created, // Submission has no reviewers yet.
    Matching, // Submission is being matched.
    Reviewing, // Submission is being reviewed.
    Accepted, // Submission has been accepted.
    Rejected // Submission has been rejected.
}
