namespace Apachi.Shared.Data;

public class LogEntry
{
    public Guid Id { get; set; }

    public required ProtocolStep Step { get; set; }

    public required byte[] Data { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
