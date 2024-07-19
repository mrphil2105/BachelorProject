namespace Apachi.Shared.Data;

public class LogEntry
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int Step { get; set; }

    public string MessageJson { get; set; } = null!;
}
