using Apachi.Shared.Crypto;

namespace Apachi.ViewModels.Services;

public static class SessionServiceExtensions
{
    public static Task<byte[]> SymmetricEncryptAndMacAsync(this ISessionService sessionService, byte[] value)
    {
        if (sessionService.AesKey == null || sessionService.HmacKey == null)
        {
            throw new InvalidOperationException("The session service must be in the logged in state.");
        }

        return EncryptionUtils.SymmetricEncryptAndMacAsync(value, sessionService.AesKey, sessionService.HmacKey);
    }

    public static Task<byte[]> SymmetricDecryptAndVerifyAsync(this ISessionService sessionService, byte[] encrypted)
    {
        if (sessionService.AesKey == null || sessionService.HmacKey == null)
        {
            throw new InvalidOperationException("The session service must be in the logged in state.");
        }

        return EncryptionUtils.SymmetricDecryptAndVerifyAsync(encrypted, sessionService.AesKey, sessionService.HmacKey);
    }
}
