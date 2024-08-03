namespace Apachi.Shared.Messages;

// 5: {|{|P;bid|}K_R^-1|}K_PCR
public class BidMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] Bid { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey, byte[] sharedKey)
    {
        var paper_Bid = SerializeByteArrays(Paper, Bid);
        var signature = await CalculateSignatureAsync(paper_Bid, reviewerPrivateKey);

        var paper_Bid_Signature = SerializeByteArrays(paper_Bid, signature);
        var encrypted = await SymmetricEncryptAsync(paper_Bid_Signature, sharedKey);
        return encrypted;
    }

    public static async Task<BidMessage> DeserializeAsync(byte[] data, byte[] sharedKey, byte[] reviewerPublicKey)
    {
        var paper_Bid_Signature = await SymmetricDecryptAsync(data, sharedKey);
        var (paper_Bid, signature) = DeserializeTwoByteArrays(paper_Bid_Signature);

        await ThrowIfInvalidSignatureAsync(paper_Bid, signature, reviewerPublicKey);
        var (paper, bid) = DeserializeTwoByteArrays(paper_Bid);

        var message = new BidMessage { Paper = paper, Bid = bid };
        return message;
    }
}
