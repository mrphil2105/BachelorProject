namespace Apachi.WebApi.Data;

// public byte[]? Signature { get; set; } // should be required I'd say

public class LogEntry
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public byte[] UserPublicKey { get; set; } = null!;
    public byte[] PreviousHash { get; set; } = null!;
    public byte[] CurrentHash { get; set; } = null!;
}