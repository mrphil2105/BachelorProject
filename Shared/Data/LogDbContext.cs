using System.Text.Json;
using Apachi.Shared.Data.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Data;

public class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options)
        : base(options) { }

    public DbSet<LogEntry> Entries => Set<LogEntry>();

    public async Task<(
        List<LogEntryResult<TMessage>> Results,
        DateTimeOffset LastCreatedDate
    )> GetEntriesAsync<TMessage>(DateTimeOffset afterDate)
        where TMessage : IMessage
    {
        var step = GetStep<TMessage>();
        var entries = await Entries.Where(entry => entry.Step == step && entry.CreatedDate > afterDate).ToListAsync();
        var lastCreatedDate = entries.Any() ? entries.Max(entry => entry.CreatedDate) : afterDate;

        var results = entries.Select(entry => CreateEntryResult<TMessage>(entry)).ToList();
        return (results, lastCreatedDate);
    }

    private static LogEntryResult<TMessage> CreateEntryResult<TMessage>(LogEntry entry)
        where TMessage : IMessage
    {
        var message = JsonSerializer.Deserialize<TMessage>(entry.MessageJson);
        return new LogEntryResult<TMessage>(entry.Id, message!);
    }

    private static int GetStep<TMessage>()
        where TMessage : IMessage
    {
        return typeof(TMessage) switch
        {
            Type type when type == typeof(SubmissionMessage) => 1,
            _ => throw new ArgumentException("Invalid step type.")
        };
    }
}
