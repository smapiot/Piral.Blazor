using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Piral.Blazor.Core
{
    public class Manipulator<T>
    {
        const BindingFlags privateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags privateStaticFlags = BindingFlags.Static | BindingFlags.NonPublic;

        private readonly ILogger<T> _logger;
        private ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>> _initializers;
        private Func<Type, Action<IServiceProvider, IComponent>> _createInitializer;

        public Manipulator(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void InitializeRenderer(WebAssemblyHost host, IServiceProvider provider)
        {
            try
            {
                var renderer = typeof(WebAssemblyHost)
                    .GetField("_renderer", privateInstanceFlags)
                    .GetValue(host);

                var componentFactory = typeof(Renderer)
                    .GetField("_componentFactory", privateInstanceFlags)
                    .GetValue(renderer);

                typeof(Renderer)
                    .GetField("_serviceProvider", privateInstanceFlags)
                    .SetValue(renderer, provider);

                var ComponentFactory = componentFactory!.GetType();

                _initializers = ComponentFactory
                    .GetField("_cachedInitializers", privateStaticFlags)
                    .GetValue(null) as ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>>;

				_createInitializer = ComponentFactory
                    .GetMethod("CreateInitializer", privateStaticFlags)
                    .CreateDelegate<Func<Type, Action<IServiceProvider, IComponent>>>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not establish local dependency injection. Error: {0}", ex);
            }
        }
        
        public void OverrideComponentInitializer(Type componentType, IServiceProvider provider)
        {
            if (componentType.IsGenericType)
            {
                _logger.LogWarning("The given component '{0}' is generic and therefore cannot be exposed to the pilet as an entry component.", componentType.Name);
                return;
            }

            try
            {
                var initializer = _createInitializer(componentType);
				_initializers.TryAdd(componentType, (_, comp) => initializer(provider, comp));
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
                _initializers.TryRemove(componentType, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not establish local dependency injection. Error: {0}", ex);
            }
        }
    }
}
