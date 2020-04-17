using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Piral.Blazor.Core
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.RootComponents
                .Add<App>("#blazor-root");

            builder.Services
                .AddBaseAddressHttpClient()
                .AddSingleton<IComponentActivationService, ComponentActivationService>()
                .AddSingleton<IModuleContainerService, ModuleContainerService>();

            await builder.Build().RunAsync();
        }
    }
}
