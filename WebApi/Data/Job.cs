namespace Apachi.WebApi.Data;

public class Job
{
    public Guid Id { get; set; }

    public JobType Type { get; set; }

    public JobStatus Status { get; set; }

    public string? Payload { get; set; }

    public string? Result { get; set; }

    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset? CompletedDate { get; set; }
}
