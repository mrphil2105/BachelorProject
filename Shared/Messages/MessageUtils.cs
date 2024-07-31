using System.Reflection;

namespace Apachi.Shared.Messages;

public static class MessageUtils
{
    private static readonly IReadOnlyDictionary<ProtocolStep, Type> _stepMessageTypes;
    private static readonly IReadOnlyDictionary<Type, ProtocolStep> _messageTypeSteps;

    static MessageUtils()
    {
        var stepMessageTypes = new Dictionary<ProtocolStep, Type>();

        var messageTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(type => type.IsClass && type.IsAssignableTo(typeof(IMessage)))
            .ToList();

        foreach (var step in Enum.GetValues<ProtocolStep>())
        {
            var messageName = step + "Message";
            var messageType = messageTypes.FirstOrDefault(type => type.Name == messageName);

            if (messageType == null)
            {
                throw new TypeLoadException($"Message type for protocol step {step} was not found.");
            }

            stepMessageTypes.Add(step, messageType);
        }

        _stepMessageTypes = stepMessageTypes;
        _messageTypeSteps = stepMessageTypes.ToDictionary(pair => pair.Value, pair => pair.Key);
    }

    public static Type MessageTypeForProtocolStep(ProtocolStep step)
    {
        if (_stepMessageTypes.TryGetValue(step, out var messageType))
        {
            return messageType;
        }

        throw new ArgumentException("Invalid protocol step.", nameof(step));
    }

    public static ProtocolStep ProtocolStepForMessageType<TMessage>()
        where TMessage : IMessage
    {
        if (_messageTypeSteps.TryGetValue(typeof(TMessage), out var step))
        {
            return step;
        }

        throw new ArgumentException("Invalid message type.");
    }

    public static ProtocolStep ProtocolStepForMessageType(Type messageType)
    {
        if (_messageTypeSteps.TryGetValue(messageType, out var step))
        {
            return step;
        }

        throw new ArgumentException("Invalid message type.");
    }
}
