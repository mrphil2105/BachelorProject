namespace Apachi.Shared.Dtos;

public record LogEntryDto(
    string Message,
    DateTime Timestamp,
    byte[]? Signature,
    Guid? UserId, // find alternative, maybe public-key,
    Guid? AdversaryId // might not be necessary
);
    
