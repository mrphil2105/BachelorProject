using System.Text.Json;
using Apachi.Shared.Data.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Data;

public class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options)
        : base(options) { }

    public DbSet<Reviewer> Reviewers => Set<Reviewer>();

    public DbSet<LogEntry> Entries => Set<LogEntry>();

    public void AddEntry<TMessage>(Guid submissionId, TMessage message)
        where TMessage : IMessage
    {
        var step = MessageUtils.ProtocolStepForMessageType<TMessage>();
        var messageJson = JsonSerializer.Serialize(message);
        var entry = new LogEntry
        {
            SubmissionId = submissionId,
            Step = step,
            MessageJson = messageJson
        };
        Entries.Add(entry);
    }

    public async Task<TMessage> GetMessageAsync<TMessage>(Guid submissionId)
        where TMessage : IMessage
    {
        var step = MessageUtils.ProtocolStepForMessageType<TMessage>();
        var entry = await Entries.SingleAsync(entry => entry.SubmissionId == submissionId && entry.Step == step);
        var message = JsonSerializer.Deserialize<TMessage>(entry.MessageJson)!;
        return message;
    }

    public async Task<List<TMessage>> GetMessagesAsync<TMessage>(Guid submissionId)
        where TMessage : IMessage
    {
        var step = MessageUtils.ProtocolStepForMessageType<TMessage>();
        var entries = await Entries
            .Where(entry => entry.SubmissionId == submissionId && entry.Step == step)
            .ToListAsync();
        var messages = entries.Select(entry => JsonSerializer.Deserialize<TMessage>(entry.MessageJson)!).ToList();
        return messages;
    }

    public async Task<(List<LogEntryResult<TMessage>> Results, DateTime LastCreatedDate)> GetEntriesAsync<TMessage>(
        DateTime afterDate
    )
        where TMessage : IMessage
    {
        var step = MessageUtils.ProtocolStepForMessageType<TMessage>();
        var entries = await Entries.Where(entry => entry.Step == step && entry.CreatedDate > afterDate).ToListAsync();
        var lastCreatedDate = entries.Any() ? entries.Max(entry => entry.CreatedDate) : afterDate;

        var results = entries.Select(entry => CreateEntryResult<TMessage>(entry)).ToList();
        return (results, lastCreatedDate);
    }

    private static LogEntryResult<TMessage> CreateEntryResult<TMessage>(LogEntry entry)
        where TMessage : IMessage
    {
        var message = JsonSerializer.Deserialize<TMessage>(entry.MessageJson);
        return new LogEntryResult<TMessage>(entry.Id, entry.SubmissionId, message!);
    }
}
