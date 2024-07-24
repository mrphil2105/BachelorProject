using System.Text.Json;
using Apachi.Shared.Data.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Data;

public class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options)
        : base(options) { }

    public DbSet<Submitter> Submitters => Set<Submitter>();

    public DbSet<Reviewer> Reviewers => Set<Reviewer>();

    public DbSet<LogEntry> Entries => Set<LogEntry>();

    public void AddMessage<TMessage>(Guid submissionId, TMessage message)
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

    public async Task<TMessage> GetMessageByEntryIdAsync<TMessage>(Guid entryId)
        where TMessage : IMessage
    {
        var step = MessageUtils.ProtocolStepForMessageType<TMessage>();
        var entry = await Entries.SingleAsync(entry => entry.Id == entryId && entry.Step == step);
        var message = JsonSerializer.Deserialize<TMessage>(entry.MessageJson)!;
        return message;
    }

    public async Task<List<LogEntryResult<TMessage>>> GetEntriesAsync<TMessage>(Guid submissionId)
        where TMessage : IMessage
    {
        var step = MessageUtils.ProtocolStepForMessageType<TMessage>();
        var entries = await Entries
            .Where(entry => entry.SubmissionId == submissionId && entry.Step == step)
            .ToListAsync();
        var results = entries
            .Select(entry =>
            {
                var message = JsonSerializer.Deserialize<TMessage>(entry.MessageJson)!;
                var result = new LogEntryResult<TMessage>(entry.Id, entry.SubmissionId, message, entry.CreatedDate);
                return result;
            })
            .ToList();
        return results;
    }

    public async Task<bool> HasMaxEntryAsync(Guid submissionId, ProtocolStep step)
    {
        var maxEntry = await Entries
            .Where(entry => entry.SubmissionId == submissionId)
            .OrderByDescending(entry => entry.Step)
            .FirstAsync();
        return maxEntry.Step == step;
    }
}
