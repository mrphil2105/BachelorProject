namespace Apachi.Shared.Dtos;

public record LogEntryDto(
    string Message,
    DateTime Timestamp,
    byte[] UserPublicKey 
);
    
