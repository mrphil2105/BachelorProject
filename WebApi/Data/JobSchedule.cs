namespace Apachi.WebApi.Data;

public class JobSchedule
{
    public Guid Id { get; set; }

    public JobType JobType { get; set; }

    public JobScheduleStatus Status { get; set; }

    public DateTimeOffset LastRun { get; set; }

    public TimeSpan Interval { get; set; }
}
