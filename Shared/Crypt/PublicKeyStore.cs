using System.Collections.Concurrent;
using System.Text.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Apachi.Shared.Crypt;

public class PublicKeyStore
{
    private const string StoreEnvironmentVariable = "PUBLIC_KEY_STORE";

    private readonly ConcurrentBag<PublicKey> _publicKeys;

    public PublicKeyStore(List<PublicKey> publicKeys)
    {
        _publicKeys = new ConcurrentBag<PublicKey>(publicKeys);
    }

    public static PublicKeyStore FromFile()
    {
        var filePath = Environment.GetEnvironmentVariable(StoreEnvironmentVariable);

        if (filePath == null)
        {
            throw new InvalidOperationException($"The {StoreEnvironmentVariable} environment variable must be set.");
        }

        if (!File.Exists(filePath))
        {
            var emptyKeyStore = new PublicKeyStore(new List<PublicKey>());
            return emptyKeyStore;
        }

        using var fileStream = File.OpenRead(filePath);
        var publicKeys = JsonSerializer.Deserialize<List<PublicKey>>(fileStream);

        if (publicKeys == null)
        {
            throw new IOException("The file cannot contain null.");
        }

        var keyStore = new PublicKeyStore(publicKeys);
        return keyStore;
    }

    public async Task ToFileAsync()
    {
        var filePath = Environment.GetEnvironmentVariable(StoreEnvironmentVariable);

        if (filePath == null)
        {
            throw new InvalidOperationException($"The {StoreEnvironmentVariable} environment variable must be set.");
        }

        await using var fileStream = File.OpenWrite(filePath);
        await JsonSerializer.SerializeAsync(fileStream, _publicKeys);
    }

    public async Task AddPublicKeyAsync(string owner, string name, ReadOnlyMemory<byte> keyBytes)
    {
        var publicKey = new PublicKey(owner, name, keyBytes);
        _publicKeys.Add(publicKey);
        await ToFileAsync();
    }

    public ECPublicKeyParameters? GetPublicKey(string owner, string name)
    {
        var publicKey = _publicKeys.FirstOrDefault(key => key.Owner == owner && key.Name == name);

        if (publicKey == null)
        {
            return null;
        }

        var keyBytes = publicKey.KeyBytes.ToArray();
        var keyParameters = (ECPublicKeyParameters)PublicKeyFactory.CreateKey(keyBytes);
        return keyParameters;
    }
}
