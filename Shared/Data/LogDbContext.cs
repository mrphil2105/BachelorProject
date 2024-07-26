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
        var messageBytes = SerializeMessage(message);
        var entry = new LogEntry
        {
            SubmissionId = submissionId,
            Step = step,
            MessageBytes = messageBytes
        };
        Entries.Add(entry);
    }

    public async Task<TMessage> GetMessageAsync<TMessage>(Guid submissionId)
        where TMessage : IMessage
    {
        var step = MessageUtils.ProtocolStepForMessageType<TMessage>();
        var entry = await Entries.SingleAsync(entry => entry.SubmissionId == submissionId && entry.Step == step);
        var message = DeserializeMessage<TMessage>(entry.MessageBytes);
        return message;
    }

    public async Task<List<TMessage>> GetMessagesAsync<TMessage>(Guid submissionId)
        where TMessage : IMessage
    {
        var step = MessageUtils.ProtocolStepForMessageType<TMessage>();
        var entries = await Entries
            .Where(entry => entry.SubmissionId == submissionId && entry.Step == step)
            .ToListAsync();
        var messages = entries.Select(entry => DeserializeMessage<TMessage>(entry.MessageBytes)).ToList();
        return messages;
    }

    public async Task<LogEntryResult<TMessage>> GetEntryAsync<TMessage>(Guid entryId)
        where TMessage : IMessage
    {
        var step = MessageUtils.ProtocolStepForMessageType<TMessage>();
        var entry = await Entries.SingleAsync(entry => entry.Id == entryId && entry.Step == step);
        var message = DeserializeMessage<TMessage>(entry.MessageBytes);
        var result = new LogEntryResult<TMessage>(entry.Id, entry.SubmissionId, message, entry.CreatedDate);
        return result;
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
                var message = DeserializeMessage<TMessage>(entry.MessageBytes);
                var result = new LogEntryResult<TMessage>(entry.Id, entry.SubmissionId, message, entry.CreatedDate);
                return result;
            })
            .ToList();
        return results;
    }

    public async Task<ProtocolStep> GetMaxProtocolStepAsync(Guid submissionId)
    {
        var maxEntry = await Entries
            .Where(entry => entry.SubmissionId == submissionId)
            .OrderByDescending(entry => entry.Step)
            .FirstAsync();
        return maxEntry.Step;
    }

    public async Task<bool> HasMaxEntryAsync(Guid submissionId, ProtocolStep step)
    {
        var maxStep = await GetMaxProtocolStepAsync(submissionId);
        return maxStep == step;
    }

    public static byte[] SerializeMessage<TMessage>(TMessage message)
        where TMessage : IMessage
    {
        var values = typeof(TMessage).GetProperties().Select(property => (byte[])property.GetValue(message)!).ToList();
        var serialized = SerializeByteArrays(values);
        return serialized;
    }

    public static TMessage DeserializeMessage<TMessage>(byte[] serialized)
        where TMessage : IMessage
    {
        var values = DeserializeByteArrays(serialized).ToArray();
        var message = (TMessage)Activator.CreateInstance(typeof(TMessage), (object[])values)!;
        return message;
    }
}
