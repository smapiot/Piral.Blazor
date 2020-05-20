using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace Piral.Blazor.Core
{
    public class ModuleContainerService : IModuleContainerService
    {
        private readonly IServiceProvider _parent;
        private readonly Manipulator<ModuleContainerService> _manipulator;

        public ModuleContainerService(IServiceProvider provider, ILogger<ModuleContainerService> logger)
        {
            _parent = provider;
            _manipulator = new Manipulator<ModuleContainerService>(logger);
            JSBridge.ContainerService = this;
        }

        public void ConfigureComponent(Type type, IServiceProvider provider) => _manipulator.OverrideComponentInitializer(type, provider);

        public void ForgetComponent(Type type) => _manipulator.RemoveComponentInitializer(type);

        public IServiceProvider Configure(Assembly assembly)
        {
            var sc = new ServiceCollection();
            var configure = assembly
                .GetTypes()
                .FirstOrDefault(x => string.Equals(x.Name, "Module", StringComparison.Ordinal))
                ?.GetMethod("ConfigureServices", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(IServiceCollection) }, null);

            configure?.Invoke(null, new[] { sc });
            var child = sc.BuildServiceProvider();
            return new NestedServiceProvider(_parent, child);
        }
    }
}
