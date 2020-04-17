using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Piral.Blazor.Core
{
    public class ModuleContainerService : IModuleContainerService
    {
        private readonly IServiceProvider _parent;
        private readonly ILogger<ModuleContainerService> _logger;
        private readonly Action<Type, IServiceProvider> _configure;

        public ModuleContainerService(IServiceProvider provider, ILogger<ModuleContainerService> logger)
        {
            _parent = provider;
            _logger = logger;
            _configure = RetrieveManipulator();
            JSBridge.ContainerService = this;
        }

        public void ConfigureComponent(Type type, IServiceProvider provider) => _configure.Invoke(type, provider);

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

        private Action<Type, IServiceProvider> RetrieveManipulator()
        {
            var factory = typeof(ComponentBase).Assembly.GetType("Microsoft.AspNetCore.Components.ComponentFactory");
            var instance = factory.GetField("Instance").GetValue(null);
            var initializers = factory.GetField("_cachedInitializers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);

            void OverrideComponentInitializer(Type componentType, IServiceProvider provider)
            {
                try
                {
                    factory.GetMethod("InstantiateComponent").Invoke(instance, new object[] { provider, componentType });
                    var converters = initializers as ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>>;
                    converters.AddOrUpdate(componentType, _ => null, (_, initializer) => (_, comp) => initializer(provider, comp));
                }
                catch (Exception ex)
                {
                    _logger.LogError("Could not establish local dependency injection. Error: {0}", ex);
                }
            }

            return OverrideComponentInitializer;
        }
    }
}
