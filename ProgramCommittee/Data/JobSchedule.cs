namespace Apachi.ProgramCommittee.Data;

public class JobSchedule
{
    public Guid Id { get; set; }

    public required JobType JobType { get; set; }

    public JobScheduleStatus Status { get; set; }

    public required TimeSpan Interval { get; set; }

    public DateTime LastRun { get; set; }
}
