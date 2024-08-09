using Apachi.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Factories;

public partial class MessageFactory : IDisposable, IAsyncDisposable
{
    private readonly LogDbContext _logDbContext;

    public MessageFactory(LogDbContext logDbContext)
    {
        _logDbContext = logDbContext;
    }

    private Task<List<LogEntry>> GetEntriesAsync(ProtocolStep step)
    {
        return _logDbContext.Entries.Where(entry => entry.Step == step).ToListAsync();
    }

    private async IAsyncEnumerable<LogEntry> EnumerateEntriesAsync(ProtocolStep step)
    {
        var entryIds = await _logDbContext
            .Entries.Where(entry => entry.Step == step)
            .Select(entry => entry.Id)
            .ToListAsync();

        foreach (var entryId in entryIds)
        {
            var entry = await _logDbContext.Entries.FirstAsync(entry => entry.Id == entryId);
            yield return entry;
        }
    }

    public void Dispose()
    {
        _logDbContext.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _logDbContext.DisposeAsync();
    }
}
