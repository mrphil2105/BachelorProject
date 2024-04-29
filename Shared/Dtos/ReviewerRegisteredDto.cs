namespace Apachi.Shared.Dtos;

public record ReviewerRegisteredDto(Guid ReviewerId, byte[] EncryptedSharedKey);
