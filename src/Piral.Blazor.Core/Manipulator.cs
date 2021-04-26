using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Piral.Blazor.Core
{
    public class Manipulator<T>
    {
        private readonly ILogger<T> _logger;
        private object _componentFactory;
        private ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>> _initializers;

        public Manipulator(ILogger<T> logger)
        {
            _logger = logger;
        }
        
        public void OverrideComponentInitializer(Type componentType, IServiceProvider provider, WebAssemblyHost host)
        {
            const BindingFlags privateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                var renderer = typeof(WebAssemblyHost)
                    .GetField("_renderer", privateInstanceFlags)
                    .GetValue(host);

                _componentFactory = typeof(Renderer)
                    .GetField("_componentFactory", privateInstanceFlags)
                    .GetValue(renderer);

                _componentFactory!.GetType()
                    .GetMethod("InstantiateComponent")
                    .Invoke(_componentFactory, new object[] { provider, componentType });

                _initializers = _componentFactory!.GetType()
                    .GetField("_cachedInitializers", privateInstanceFlags)
                    .GetValue(_componentFactory) as ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>>;

                _initializers.AddOrUpdate(componentType, _ => null,
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
                _initializers.TryRemove(componentType, out var _);
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not establish local dependency injection. Error: {0}", ex);
            }
        }
    }
}
