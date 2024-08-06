namespace Apachi.Shared.Messages;

// 12: {|{|D|}K_R^-1|}K_P
public class DiscussionMessage : IMessage
{
    public required byte[] Message { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey, byte[] groupKey)
    {
        var signature = await CalculateSignatureAsync(Message, reviewerPrivateKey);

        var message_Signature = SerializeByteArrays(Message, signature);
        var encrypted = await SymmetricEncryptAsync(message_Signature, groupKey);
        return encrypted;
    }

    public static async Task<DiscussionMessage> DeserializeAsync(byte[] data, byte[] groupKey, byte[] reviewerPublicKey)
    {
        var message_Signature = await SymmetricDecryptAsync(data, groupKey);
        var (messageBytes, signature) = DeserializeTwoByteArrays(message_Signature);

        await ThrowIfInvalidSignatureAsync(messageBytes, signature, reviewerPublicKey);

        var message = new DiscussionMessage { Message = messageBytes };
        return message;
    }
}
