using System.Text.Json;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared.Data;
using Apachi.Shared.Data.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee.Services;

public abstract class MessageProcessor<TMessage, TResponseMessage> : IJobProcessor
    where TMessage : IMessage
    where TResponseMessage : IMessage
{
    private readonly LogDbContext _logDbContext;

    protected MessageProcessor(LogDbContext logDbContext)
    {
        _logDbContext = logDbContext;
    }

    public abstract Task<TResponseMessage> ProcessMessageAsync(TMessage message, CancellationToken cancellationToken);

    async Task IJobProcessor.ProcessJobAsync(Job job, CancellationToken cancellationToken)
    {
        var step = MessageUtils.ProtocolStepForMessageType<TMessage>();
        var logEntry = await _logDbContext.Entries.SingleAsync(entry =>
            entry.SubmissionId == job.SubmissionId && entry.Step == step
        );
        var message = JsonSerializer.Deserialize<TMessage>(logEntry.MessageJson)!;

        var responseMessage = await ProcessMessageAsync(message, cancellationToken);
        _logDbContext.AddEntry(job.SubmissionId, responseMessage);
        await _logDbContext.SaveChangesAsync();
    }
}
