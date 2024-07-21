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
        var message = await _logDbContext.GetMessageAsync<TMessage>(job.SubmissionId);
        var responseMessage = await ProcessMessageAsync(message, cancellationToken);
        _logDbContext.AddMessage(job.SubmissionId, responseMessage);
        await _logDbContext.SaveChangesAsync();
    }
}
