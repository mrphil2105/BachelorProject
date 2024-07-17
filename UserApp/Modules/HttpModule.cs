using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Apachi.UserApp.Modules;

public class HttpModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var endpoint = Environment.GetEnvironmentVariable("APACHI_ENDPOINT") ?? "http://localhost:5144";
        var services = new ServiceCollection();
        services.AddHttpClient(
            string.Empty,
            client =>
            {
                client.BaseAddress = new Uri(endpoint);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            }
        );
        builder.Populate(services);
    }
}
