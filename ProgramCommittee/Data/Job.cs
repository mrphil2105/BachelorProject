namespace Apachi.ProgramCommittee.Data;

public class Job
{
    public Guid Id { get; set; }

    public required JobType Type { get; set; }

    public JobStatus Status { get; set; }

    public required Guid SubmissionId { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedDate { get; set; }
}
