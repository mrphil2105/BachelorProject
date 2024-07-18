namespace Apachi.ProgramCommittee.Data;

public class Job
{
    public Guid Id { get; set; }

    public JobType Type { get; set; }

    public JobStatus Status { get; set; }

    public string? Payload { get; set; }

    public string? Result { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedDate { get; set; }
}
