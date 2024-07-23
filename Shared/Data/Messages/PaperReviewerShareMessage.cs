namespace Apachi.Shared.Data.Messages;

public record PaperReviewerShareMessage(byte[] EncryptedPaper, byte[] PaperSignature) : IMessage;
