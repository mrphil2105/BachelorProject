using Apachi.Shared;

namespace Apachi.UserApp.Data;

public class LogEvent
{
    public Guid Id { get; set; }

    public required ProtocolStep Step { get; set; }

    public required byte[] Identifier { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public required Guid ReviewerId { get; set; }

    public Reviewer Reviewer { get; set; } = null!;
}
