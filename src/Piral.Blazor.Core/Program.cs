using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Piral.Blazor.Core.Dependencies;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Piral.Blazor.Core;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
        var factory = new PiralServiceProviderFactory();

        builder.RootComponents.Add<App>("#blazor-root");
        builder.RootComponents.RegisterCustomElement<Element>("piral-blazor-component");

        builder.Services
            .AddSingleton(new HttpClient { BaseAddress = baseAddress })
            .AddSingleton<IComponentActivator, PiletComponentActivator>()
            .AddSingleton<IComponentActivationService, ComponentActivationService>()
            .AddSingleton<IModuleContainerService, ModuleContainerService>()
            .AddSingleton<IQueryParameterValueProvider, QueryParameterValueProvider>();

        builder.ConfigureLogger();
        builder.ConfigureContainer(factory);

        var host = builder.Build();

        JSBridge.Initialize(host);
        
        await host.RunAsync();
    }
}
