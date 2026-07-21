using Microsoft.AspNetCore.Components;
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
    private static readonly ICacheManipulatorService NoopCacheManipulator = new NoopCacheManipulatorService();

    private readonly IPiralServiceProvider _provider;
    private readonly ICacheManipulatorService _cacheManipulator;
    private readonly Dictionary<AssemblyLoadContext, IServiceProvider> _providers = new();

    public ModuleContainerService(IPiralServiceProvider provider)
        : this(provider, NoopCacheManipulator)
    {
    }

    public ModuleContainerService(IPiralServiceProvider provider, ICacheManipulatorService cacheManipulator)
    {
        _provider = provider;
        _cacheManipulator = cacheManipulator ?? NoopCacheManipulator;
        _providers.Add(AssemblyLoadContext.Default, _provider);
    }

    public IServiceProvider ConfigureModule(Assembly assembly, IPiletService pilet)
    {
        var services = new ServiceCollection();
        var alc = AssemblyLoadContext.GetLoadContext(assembly);

        ConfigureGlobalServices(services, assembly, pilet);
        ConfigureLocalServices(services, assembly, pilet);
        ConfigureDefaultServices(services, assembly, pilet);

        var provider = CreateProvider(alc, services);
        PrewarmComponentCache(assembly, provider);
        return _providers[alc] = provider;
    }

    private IServiceProvider CreateProvider(AssemblyLoadContext alc, ServiceCollection services)
    {
        if (alc == AssemblyLoadContext.Default)
        {
            return _provider.ExtendGlobalServiceProvider(services);
        }
        else
        {
            return _provider.CreatePiletServiceProvider(services);
        }
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

    private void PrewarmComponentCache(Assembly assembly, IServiceProvider provider)
    {
        foreach (var componentType in assembly.GetTypes().Where(m => !m.IsAbstract && typeof(IComponent).IsAssignableFrom(m)))
        {
            _cacheManipulator.UpdateComponentCache(componentType, provider);
        }
    }

    private sealed class NoopCacheManipulatorService : ICacheManipulatorService
    {
        public void UpdateComponentCache(Type componentType, IServiceProvider provider)
        {
        }
    }
}
