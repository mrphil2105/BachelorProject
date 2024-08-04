using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public class MessageCreationException : Exception
{
    public MessageCreationException(ProtocolStep step)
        : base($"Unable to create message of type {MessageUtils.MessageTypeForProtocolStep(step).Name}.")
    {
        Step = step;
    }

    public ProtocolStep Step { get; }
}
