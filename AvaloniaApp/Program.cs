using Apachi.Shared.Crypt;
using Avalonia;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;

namespace Apachi.AvaloniaApp;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var curveName = SecObjectIdentifiers.SecP521r1.Id;
        var keyPair = KeyUtils.GenerateKeyPair("P-521");
        var publicKey = (ECPublicKeyParameters)keyPair.Public;
        var keyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);
        var keyBytes = keyInfo.GetEncoded();
        File.WriteAllBytes("/tmp/public-key", keyBytes);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
