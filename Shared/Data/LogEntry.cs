namespace Apachi.Shared.Data;

public class LogEntry
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    public int Step { get; set; }

    public string MessageJson { get; set; } = null!;
}
