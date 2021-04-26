using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Piral.Blazor.Core
{
    public class Manipulator<T>
    {
        private readonly ILogger<T> _logger;
        private readonly Type _factoryType;
        private readonly object _factoryInstance;
        private readonly object _initializers;

        public Manipulator(ILogger<T> logger)
        {
            _logger = logger;

            var activatorType = typeof(ComponentBase).Assembly
                .GetTypes()
                .Single(t => t.Name.Equals("DefaultComponentActivator"));

            var activatorInstance = activatorType
                .GetProperty("Instance")
                .GetValue(null); //static value
            
            _factoryType = typeof(ComponentBase).Assembly
                .GetTypes().Single(t => t.Name.Equals("ComponentFactory"));

            _factoryInstance = _factoryType
                .GetConstructor(new[] { activatorType })
                .Invoke(new[] { activatorInstance });

            _initializers = _factoryType
                .GetField("_cachedInitializers", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_factoryInstance);
        }

        public void OverrideComponentInitializer(Type componentType, IServiceProvider provider)
        {
            try
            {
                _factoryType.GetMethod("InstantiateComponent")
                    .Invoke(_factoryInstance, new object[] { provider, componentType });
                var converters = _initializers as ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>>;
                converters.AddOrUpdate(componentType, _ => null,
                    (_, initializer) => (_, comp) => initializer(provider, comp));
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
