using System.Reflection;
using Apachi.ProgramCommittee.Data;
using Apachi.ProgramCommittee.Services;
using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee;

public class Program
{
    private static readonly CancellationTokenSource _cancellationSource = new();

    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--generate-keypair")
        {
            await PrintKeyPairAsync();
            return;
        }

        Console.CancelKeyPress += OnCancelKeyPress;

        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
        var container = containerBuilder.Build();

        await PrepareAsync(container);
        var calculatorRunner = container.Resolve<CalculatorRunner>();

        try
        {
            await calculatorRunner.ExecuteAsync(_cancellationSource.Token);
        }
        catch (OperationCanceledException) { }
    }

    private static async Task PrepareAsync(ILifetimeScope container)
    {
        using (var lifetimeScope = container.BeginLifetimeScope())
        using (var dbContext = lifetimeScope.Resolve<AppDbContext>())
        {
            await dbContext.Database.MigrateAsync();
        }
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        if (_cancellationSource.IsCancellationRequested)
        {
            return;
        }

        Console.WriteLine("Exiting gracefully... Press Ctrl+C again to exit forcefully.");
        e.Cancel = true;
        _cancellationSource.Cancel();
    }

    private static async Task PrintKeyPairAsync()
    {
        var (privateKey, publicKey) = await GenerateKeyPairAsync();
        var privateKeyBase64 = Convert.ToBase64String(privateKey);
        var publicKeyBase64 = Convert.ToBase64String(publicKey);
        Console.WriteLine("APACHI_PC_PRIVATE_KEY: {0}", privateKeyBase64);
        Console.WriteLine("APACHI_PC_PUBLIC_KEY: {0}", publicKeyBase64);
    }
}
