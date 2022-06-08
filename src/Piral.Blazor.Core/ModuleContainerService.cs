using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        public IServiceProvider ConfigureModule(Assembly assembly)
        {
            var globalServices = ConfigureGlobalServices(assembly);
            var piletServices = ConfigurePiletServices(assembly);

            _provider.AddGlobalServices(globalServices);
            return _provider.CreatePiletServiceProvider(piletServices);
        }

        private static IServiceCollection ConfigureGlobalServices(Assembly assembly)
        {
            var sc = new ServiceCollection();
            var configure = assembly
                .GetTypes()
                .FirstOrDefault(x => string.Equals(x.Name, "Module", StringComparison.Ordinal))
                ?.GetMethod("ConfigureShared", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(IServiceCollection) }, null);

            configure?.Invoke(null, new[] { sc });
            return sc;
        }

        private static IServiceCollection ConfigurePiletServices(Assembly assembly)
        {
            var globalServices = ConfigureGlobal(assembly);
            var piletServices = ConfigurePilet(assembly);

            _provider.AddGlobalServices(globalServices);
            return _provider.CreatePiletServiceProvider(piletServices);
        }

        private static IServiceCollection ConfigureGlobal(Assembly assembly)
        {
            var sc = new ServiceCollection();
            var configure = assembly
                .GetTypes()
                .FirstOrDefault(x => string.Equals(x.Name, "Module", StringComparison.Ordinal))
                ?.GetMethod("ConfigureShared", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(IServiceCollection) }, null);

            configure?.Invoke(null, new[] { sc });
            return sc;
        }

        private static IServiceCollection ConfigurePilet(Assembly assembly)
        {
            var sc = new ServiceCollection();
            var configure = assembly
                .GetTypes()
                .FirstOrDefault(x => string.Equals(x.Name, "Module", StringComparison.Ordinal))
                ?.GetMethod("ConfigureServices", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(IServiceCollection) }, null);

            configure?.Invoke(null, new[] { sc });
            return sc;
        }
    }
}
