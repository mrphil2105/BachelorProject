namespace Apachi.WebApi.Data;

public class Job
{
    public Guid Id { get; set; }

    public JobType Type { get; set; }

    public JobStatus Status { get; set; }

    public string? Payload { get; set; }

    public string? Result { get; set; }

    public DateTimeOffset ScheduleDate { get; set; }

    public DateTimeOffset? StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }
}
