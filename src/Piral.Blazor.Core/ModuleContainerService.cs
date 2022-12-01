using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Piral.Blazor.Utils;
using System;
using System.Linq;
using System.Reflection;

namespace Piral.Blazor.Core
{
    public class ModuleContainerService : IModuleContainerService
    {
        private readonly IPiralServiceProvider _provider;

        private readonly Manipulator<ModuleContainerService> _manipulator;

        public ModuleContainerService(IPiralServiceProvider provider, ILogger<ModuleContainerService> logger)
        {
            _provider = provider;
            _manipulator = new Manipulator<ModuleContainerService>(logger);
        }

        public void ConfigureHost(WebAssemblyHost host)
        {
            _manipulator.InitializeRenderer(host, _provider);
        }

        public void ConfigureComponent(Type type, IServiceProvider provider)
        {
            _manipulator.OverrideComponentInitializer(type, provider);
        }

        public void ForgetComponent(Type type) => _manipulator.RemoveComponentInitializer(type);

        public IServiceProvider ConfigureModule(Assembly assembly, IPiletService pilet)
        {
            var globalServices = ConfigureGlobalServices(assembly, pilet.Config);
            var piletServices = ConfigurePiletServices(assembly, pilet.Config)
                .AddSingleton(pilet);
            _provider.AddGlobalServices(globalServices);
            return _provider.CreatePiletServiceProvider(piletServices);
        }

        private static IServiceCollection ConfigureGlobalServices(Assembly assembly, IConfiguration cfg)
        {
            var sc = new ServiceCollection();

            FindMethod(assembly, "ConfigureShared", typeof(IServiceCollection))
                ?.Invoke(null, new Object[] { sc });
            
            FindMethod(assembly, "ConfigureShared", typeof(IServiceCollection), typeof(IConfiguration))
                ?.Invoke(null, new Object[] { sc, cfg });
            
            return sc;
        }

        private static IServiceCollection ConfigurePiletServices(Assembly assembly, IConfiguration cfg)
        {
            var sc = new ServiceCollection();

            FindMethod(assembly, "ConfigureServices", typeof(IServiceCollection))
                ?.Invoke(null, new Object[] { sc });

            FindMethod(assembly, "ConfigureServices", typeof(IServiceCollection), typeof(IConfiguration))
                ?.Invoke(null, new Object[] { sc, cfg });

            return sc;
        }

        private static MethodInfo FindMethod(Assembly assembly, string name, params Type[] parameters)
        {
            return assembly
                .GetTypes()
                .FirstOrDefault(x => string.Equals(x.Name, "Module", StringComparison.Ordinal))
                ?.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
        }
    }
}
