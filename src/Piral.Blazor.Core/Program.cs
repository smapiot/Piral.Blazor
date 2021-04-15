using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Piral.Blazor.Core
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

            builder.RootComponents
                .Add<App>("#blazor-root");

            builder.Services
                .AddSingleton(new HttpClient { BaseAddress = baseAddress })
                .AddSingleton<IComponentActivationService, ComponentActivationService>()
                .AddSingleton<IModuleContainerService, ModuleContainerService>();

            var host = builder.Build();

            Configure(host);
            
            await host.RunAsync();
        }

        private static void Configure(WebAssemblyHost host)
        {
            JSBridge.Configure(host.Services.GetRequiredService<HttpClient>());
        }
    }
}
