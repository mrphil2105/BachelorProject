namespace Apachi.WebApi.Data;

public class LogEntry
{
    public Guid Id { get; set; }
    
    public string Message { get; set; } = null!;
    
    public DateTime Timestamp { get; set; }
    
    public LogType Type { get; set; }
    
    public Guid UserId { get; set; } // gotta rethink this
    
    public Guid? AdversaryId { get; set; } // gotta rethink this
}