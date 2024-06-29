using Apachi.Shared;

namespace Apachi.WebApi.Data;

public class Review
{
    public Guid Id { get; set; }

    public ReviewStatus Status { get; set; }

    public Guid ReviewerId { get; set; }

    public Reviewer Reviewer { get; set; } = null!;

    public Guid SubmissionId { get; set; }

    public Submission Submission { get; set; } = null!;
}
