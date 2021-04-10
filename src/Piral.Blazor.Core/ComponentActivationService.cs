using Microsoft.Extensions.Logging;
using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Piral.Blazor.Core
{
    public class ComponentActivationService : IComponentActivationService
    {
        private readonly Dictionary<string, Type> _services = new Dictionary<string, Type>();
        private readonly List<Assembly> _assemblies = new List<Assembly>();
        
        private readonly List<ActiveComponent> _active = new List<ActiveComponent>();
        private readonly ILogger<ComponentActivationService> _logger;
        private readonly IModuleContainerService _container;

        public event EventHandler Changed;

        public IEnumerable<ActiveComponent> Components => _active;

        public ComponentActivationService(IModuleContainerService container, ILogger<ComponentActivationService> logger)
        {
            _container = container;
            _logger = logger;
            JSBridge.ActivationService = this;
        }

        public void Register(string componentName, Type componentType, IServiceProvider provider)
        {
            if (_services.ContainsKey(componentName))
            {
                _logger.LogWarning("The provided component name has already been registered.");
            }
            else
            {
                _services.Add(componentName, componentType);
                _container.ConfigureComponent(componentType, provider);
            }
        }

        public void Unregister(string componentName)
        {
            if (_services.TryGetValue(componentName, out var componentType))
            {
                DeactivateComponent(componentName);
                _services.Remove(componentName);
                _container.ForgetComponent(componentType);
            }
            else
            {
                _logger.LogWarning("The provided component name has not been registered.");
            }
        }

        public void ActivateComponent(string componentName, string referenceId, IDictionary<string, object> args)
        {
            var component = GetComponent(componentName);
            _active.Add(new ActiveComponent(componentName, referenceId, component, args));
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void DeactivateComponent(string componentName, string referenceId)
        {
            var removed = _active.RemoveAll(m => m.ComponentName == componentName && m.ReferenceId == referenceId);

            if (removed > 0)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void DeactivateComponent(string componentName)
        {
            var removed = _active.RemoveAll(m => m.ComponentName == componentName);

            if (removed > 0)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private Type GetComponent(string componentName)
        {
            if (!_services.TryGetValue(componentName, out var value))
            {
                return LoadMissingComponentsFor(componentName);
            }
            else
            {
                return value;
            }
        }

        private Type LoadMissingComponentsFor(string componentName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Except(_assemblies).ToArray();
            var result = default(Type);

            foreach (var assembly in assemblies)
            {
                var serviceProvider = _container.Configure(assembly);
                var types = assembly.GetTypes().Where(m => m.GetCustomAttribute<ExposePiletAttribute>(false) != null);

                foreach (var type in types)
                {
                    var name = type.GetCustomAttribute<ExposePiletAttribute>(false).Name;
                    Register(name, type, serviceProvider);

                    if (name == componentName)
                    {
                        result = type;
                    }
                }

                _assemblies.Add(assembly);
            }

            return result;
        }
    }
}
