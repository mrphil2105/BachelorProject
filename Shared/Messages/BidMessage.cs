namespace Apachi.Shared.Messages;

// 5: {|{|P;bid|}K_R^-1|}K_PCR
public class BidMessage : IMessage
{
    public required byte[] Paper { get; init; }

    public required byte[] Bid { get; init; }

    public async Task<byte[]> SerializeAsync(byte[] reviewerPrivateKey, byte[] sharedKey)
    {
        var paperAndBid = SerializeByteArrays(Paper, Bid);
        var signature = await CalculateSignatureAsync(paperAndBid, reviewerPrivateKey);

        var paperAndBidAndSignature = SerializeByteArrays(paperAndBid, signature);
        var encryptedPaperAndBidAndSignature = await SymmetricEncryptAsync(paperAndBidAndSignature, sharedKey);
        return encryptedPaperAndBidAndSignature;
    }

    public static async Task<BidMessage> DeserializeAsync(byte[] data, byte[] sharedKey, byte[] reviewerPublicKey)
    {
        var paperAndBidAndSignature = await SymmetricDecryptAsync(data, sharedKey);
        var (paperAndBid, signature) = DeserializeTwoByteArrays(paperAndBidAndSignature);

        await ThrowIfInvalidSignatureAsync(paperAndBid, signature, reviewerPublicKey);
        var (paper, bid) = DeserializeTwoByteArrays(paperAndBid);

        var message = new BidMessage { Paper = paper, Bid = bid };
        return message;
    }
}
