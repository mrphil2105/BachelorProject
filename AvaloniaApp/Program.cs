using System;
using Apachi.Shared.Crypt;
using Avalonia;

namespace Apachi.AvaloniaApp;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main()
    {
        //BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        
        byte[] paper = [1, 2, 3, 4, 5];
        byte[] identity = [6, 7, 8, 9, 10];
        byte[] alteredIdentity = [6, 7, 8, 9, 11];
        
        Commitment paperCommitment = Commitment.Create(paper);
        Commitment identityCommitment = Commitment.Create(identity);

        var isPCommitValid = paperCommitment.MatchesValue(paper);
        Console.WriteLine($"Paper commit is valid: {isPCommitValid}");

        var isICommitValid = identityCommitment.MatchesValue(identity);
        Console.WriteLine($"Identity commit is valid: {isICommitValid}");
        
        var isICommitValidWrong = identityCommitment.MatchesValue(alteredIdentity);
        Console.WriteLine($"Identity commit is valid: {isICommitValidWrong}");

        var lol = paperCommitment.ToBytes();
        var lol2 = Commitment.FromBytes(lol);
        var isFromBytesPaperValid = lol2.MatchesValue(paper);
        Console.WriteLine($"Paper ToBytes commit is valid: {isFromBytesPaperValid}");

        var meow = identityCommitment.ToBytes();
        var meow2 = Commitment.FromBytes(meow);
        var isFromBytesIdentityValid = meow2.MatchesValue(identity);
        Console.WriteLine($"Identity ToBytes commit is valid: {isFromBytesIdentityValid}");
        
        var cock = identityCommitment.ToBytes();
        var cock2 = Commitment.FromBytes(cock);
        var isFromBytesIdentityWrong = cock2.MatchesValue(paper);
        Console.WriteLine($"Identity ToBytes commit is valid: {isFromBytesIdentityWrong}");
    }
    
    

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
