using Microsoft.Extensions.Logging;
using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Piral.Blazor.Core
{
    public class ComponentActivationService : IComponentActivationService
    {
        private readonly Dictionary<string, Type> _services = new Dictionary<string, Type>();
        
        private readonly List<ActiveComponent> _active = new List<ActiveComponent>();
        private readonly ILogger<ComponentActivationService> _logger;
        private readonly IModuleContainerService _container;

        public event EventHandler Changed;

        public IEnumerable<ActiveComponent> Components => _active;
        
        private static readonly IReadOnlyCollection<Type> AttributeTypes = new List<Type>
        {
            typeof(ExposePiletAttribute),
            typeof(RouteAttribute)
        };

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

        public void LoadComponentsFromAssembly(Assembly assembly, IDictionary<string, object> args = default)
        {
            var serviceProvider = _container.Configure(assembly);
            
            var attributeTypes = FilterAttributeTypes(args);
            var componentTypes = assembly.GetTypesWithAttributes(attributeTypes);

            foreach (var componentType in componentTypes)
            {
                var attributeValues = GetAllAttributeValues(componentType, attributeTypes);
                if (attributeValues is null) continue;

                foreach (string componentName in attributeValues)
                {
                    string cleanComponentName = Sanitize(componentName);
                    Register(cleanComponentName, componentType, serviceProvider);
                    _logger.LogInformation($"registered {componentName}");
                }
            }
        }
        
        /// <summary>
        /// Sanitizing a Blazor component name. Any leading slashes are removed and
        /// everything that is not alphanumeric, an underscore or a dash gets replaced with an underscore.
        /// </summary>
        public static string Sanitize(string value)
        {
            string val = value.StartsWith("/") ? value.Substring(1) : value;
            return Regex.Replace(val, @"[^\w\-]", "_");
        }

        private Type GetComponent(string componentName)
        {
            _services.TryGetValue(componentName, out var value);
            return value;
        }
        
        private IReadOnlyCollection<Type> FilterAttributeTypes(IDictionary<string, object> args)
        {
            var types = AttributeTypes;

            types = FilterPages(args, types);
            // potentially more filters here

            return types;
        }

        private IReadOnlyCollection<Type> FilterPages(IDictionary<string, object> args, IReadOnlyCollection<Type> types)
        {
            if (args.HasExplicitFlag(true, "includePages"))
                return types; //don't filter anything out

            _logger.LogInformation(
                "Pages decorated with the @page directive are not included in the Blazor references. If this is unintended, reference the docs."
            );

            return types.Where(at => at != typeof(RouteAttribute)).ToList().AsReadOnly(); // filter out the pages
        }
        
        private static IEnumerable<string> GetAllAttributeValues(Type member, IEnumerable<Type> attributeTypes)
        {
            return attributeTypes.Select(at=> GetAttributeValue(member, at)).Where(val => val != null);
        }

        private static string GetAttributeValue(Type member, Type attributeType)
        {
            string value = attributeType.Name switch
            {
                nameof(RouteAttribute) => member.GetCustomAttribute<RouteAttribute>(false)?.Template,
                nameof(ExposePiletAttribute) => member.GetCustomAttribute<ExposePiletAttribute>(false)?.Name,
                _ => null
            };

            return value;
        }
    }
}
