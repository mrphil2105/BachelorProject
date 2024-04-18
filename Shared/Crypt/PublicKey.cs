namespace Apachi.Shared.Crypt;

public record PublicKey(string Owner, string Name, ReadOnlyMemory<byte> KeyBytes);
