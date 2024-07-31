using Apachi.Shared;

namespace Apachi.ProgramCommittee.Data;

public class LogEvent
{
    public Guid Id { get; set; }

    public required ProtocolStep Step { get; set; }

    public required byte[] Identifier { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
