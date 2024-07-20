namespace Apachi.Shared.Data;

public class LogEntry
{
    public Guid Id { get; set; }

    public required Guid SubmissionId { get; set; }

    public required ProtocolStep Step { get; set; }

    public required string MessageJson { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
