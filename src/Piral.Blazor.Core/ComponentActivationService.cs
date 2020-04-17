using System;
using System.Collections.Generic;

namespace Piral.Blazor.Core
{
    public class ComponentActivationService : IComponentActivationService
    {
        private readonly Dictionary<string, Type> _services = new Dictionary<string, Type>();
        private readonly List<ActiveComponent> _active = new List<ActiveComponent>();
        private readonly IModuleContainerService _container;

        public event EventHandler Changed;

        public IEnumerable<ActiveComponent> Components => _active;

        public ComponentActivationService(IModuleContainerService container)
        {
            _container = container;
            JSBridge.ActivationService = this;
        }

        public void Register(string componentName, Type componentType, IServiceProvider provider)
        {
            if (_services.ContainsKey(componentName))
            {
                throw new InvalidOperationException("The provided component name has already been registered.");
            }

            _services.Add(componentName, componentType);
            _container.ConfigureComponent(componentType, provider);
        }

        public void ActivateComponent(string componentName, string referenceId, IDictionary<string, object> args)
        {
            _active.Add(new ActiveComponent(componentName, referenceId, GetComponent(componentName), args));
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

        private Type GetComponent(string componentName)
        {
            _services.TryGetValue(componentName, out var value);
            return value;
        }
    }
}
