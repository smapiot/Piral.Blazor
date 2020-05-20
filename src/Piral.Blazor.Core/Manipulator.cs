using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Piral.Blazor.Core
{
    public class Manipulator<T>
    {
        private readonly ILogger<T> _logger;
        private readonly Type _factory;
        private readonly object _instance;
        private readonly object _initializers;

        public Manipulator(ILogger<T> logger)
        {
            _logger = logger;
            _factory = typeof(ComponentBase).Assembly.GetType("Microsoft.AspNetCore.Components.ComponentFactory");
            _instance = _factory.GetField("Instance").GetValue(null);
            _initializers = _factory.GetField("_cachedInitializers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_instance);
        }

        public void OverrideComponentInitializer(Type componentType, IServiceProvider provider)
        {
            try
            {
                _factory.GetMethod("InstantiateComponent").Invoke(_instance, new object[] { provider, componentType });
                var converters = _initializers as ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>>;
                converters.AddOrUpdate(componentType, _ => null, (_, initializer) => (_, comp) => initializer(provider, comp));
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not establish local dependency injection. Error: {0}", ex);
            }
        }

        public void RemoveComponentInitializer(Type componentType)
        {
            try
            {
                var converters = _initializers as ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>>;
                converters.TryRemove(componentType, out var _);
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not establish local dependency injection. Error: {0}", ex);
            }
        }
    }
}
