using Apachi.Shared.Data.Messages;

namespace Apachi.Shared.Data;

public record LogEntryResult<TMessage>(Guid Id, Guid SubmissionId, TMessage Message, DateTime CreatedDate)
    where TMessage : IMessage;
