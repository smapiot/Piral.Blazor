using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Piral.Blazor.Core;

public class ModuleContainerService : IModuleContainerService
{
    private readonly IPiralServiceProvider _provider;
    private readonly Dictionary<AssemblyLoadContext, IServiceProvider> _providers = new();

    public ModuleContainerService(IPiralServiceProvider provider)
    {
        _provider = provider;
    }

    public void ConfigureModule(Assembly assembly, IPiletService pilet)
    {
        var services = new ServiceCollection();
        var alc = AssemblyLoadContext.GetLoadContext(assembly);

        ConfigureGlobalServices(services, assembly, pilet);
        ConfigureLocalServices(services, assembly, pilet);
        ConfigureDefaultServices(services, assembly, pilet);

        _providers.Add(alc, _provider.CreatePiletServiceProvider(services));
    }

    private static void ConfigureDefaultServices(ServiceCollection services, Assembly assembly, IPiletService pilet)
    {
        services.AddSingleton(pilet);
    }

    public IServiceProvider GetProvider(Assembly assembly)
    {
        var alc = AssemblyLoadContext.GetLoadContext(assembly);

        if (alc is not null)
        {
            return _providers.GetValueOrDefault(alc);
        }

        return _provider;
    }

    private static void ConfigureGlobalServices(IServiceCollection sc, Assembly assembly, IPiletService pilet)
    {
        var cfg = pilet.Config;

        FindMethod(assembly, "ConfigureShared", typeof(IServiceCollection))
            ?.Invoke(null, new Object[] { sc });
        
        FindMethod(assembly, "ConfigureShared", typeof(IServiceCollection), typeof(IConfiguration))
            ?.Invoke(null, new Object[] { sc, cfg });
    }

    private static void ConfigureLocalServices(IServiceCollection sc, Assembly assembly, IPiletService pilet)
    {
        var cfg = pilet.Config;

        FindMethod(assembly, "ConfigureServices", typeof(IServiceCollection))
            ?.Invoke(null, new Object[] { sc });

        FindMethod(assembly, "ConfigureServices", typeof(IServiceCollection), typeof(IConfiguration))
            ?.Invoke(null, new Object[] { sc, cfg });
    }

    private static MethodInfo FindMethod(Assembly assembly, string name, params Type[] parameters)
    {
        return assembly
            .GetTypes()
            .FirstOrDefault(x => string.Equals(x.Name, "Module", StringComparison.Ordinal))
            ?.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
    }
}
