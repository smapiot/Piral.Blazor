using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Piral.Blazor.Core
{
    public class ComponentActivationService : IComponentActivationService
    {
        private static readonly string appRoot = "approot";

        private readonly Dictionary<string, Type> _services = new Dictionary<string, Type>();

        private readonly Dictionary<string, PiralElement> _elements = new Dictionary<string, PiralElement>();

        private readonly List<ActiveComponent> _active = new List<ActiveComponent>();

        private readonly ILogger<ComponentActivationService> _logger;

        private readonly IModuleContainerService _container;

        public event EventHandler ComponentsChanged;

        public event EventHandler RootChanged;

        public event EventHandler LanguageChanged;

        public IEnumerable<ActiveComponent> Components => _active;

        public Type Root => GetComponent(appRoot) ?? typeof(DefaultRoot);

        public string Language
        {
            get
            {
                return CultureInfo.CurrentCulture.Name;
            }
            set
            {
                var culture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.Name == value);

                if (culture is not null)
                {
                    CultureInfo.DefaultThreadCurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                }

                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static readonly IReadOnlyCollection<Type> AttributeTypes = new List<Type>
        {
            typeof(PiralAppRootAttribute),
            typeof(PiralComponentAttribute),
            typeof(PiralExtensionAttribute),
            typeof(RouteAttribute)
        };

        public ComponentActivationService(IModuleContainerService container, ILogger<ComponentActivationService> logger)
        {
            _container = container;
            _logger = logger;
            JSBridge.ActivationService = this;
            container.ConfigureHost(JSBridge.Host);
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

                if (componentName == appRoot)
                {
                    RootChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void Unregister(string componentName)
        {
            if (_services.TryGetValue(componentName, out var componentType))
            {
                DeactivateComponent(componentName);
                _services.Remove(componentName);
                _container.ForgetComponent(componentType);

                if (componentName == appRoot)
                {
                    RootChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                _logger.LogWarning("The provided component name has not been registered.");
            }
        }

        public PiralElement GetElement(string referenceId)
        {
            if (_elements.TryGetValue(referenceId, out var element))
            {
                return element;
            }

            return null;
        }

        public void MountComponent(string componentName, string referenceId, IDictionary<string, JsonElement> args)
        {
            var component = GetComponent(componentName);

            try
            {
                var element = new PiralElement(component, args);
                _elements.TryAdd(referenceId, element);
            }
            catch (ArgumentException ae)
            {
                _logger.LogError($"One of the arguments is invalid: {ae.Message}");
            }
        }

        public void UnmountComponent(string referenceId)
        {
            if (_elements.Remove(referenceId, out var element))
            {
                element.HasChanged();
            }
        }

        public void UpdateComponent(string referenceId, IDictionary<string, JsonElement> args)
        {
            if (_elements.TryGetValue(referenceId, out var element))
            {
                element.UpdateArgs(args);
            }
        }

        public void ActivateComponent(string componentName, string referenceId, IDictionary<string, JsonElement> args)
        {
            var component = GetComponent(componentName);

            try
            {
                _active.Add(new ActiveComponent(componentName, referenceId, component, args));
                ComponentsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (ArgumentException ae)
            {
                _logger.LogError($"One of the arguments is invalid: {ae.Message}");
            }
        }

        public void DeactivateComponent(string componentName, string referenceId)
        {
            var removed = RemoveActivations(m => m.ComponentName == componentName && m.ReferenceId == referenceId);

            if (removed > 0)
            {
                ComponentsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ReactivateComponent(string componentName, string referenceId, IDictionary<string, JsonElement> args)
        {
            for (var i = 0; i < _active.Count; i++)
            {
                var component = _active[i];

                if (component.ComponentName == componentName && component.ReferenceId == referenceId)
                {
                    _active[i] = new ActiveComponent(componentName, referenceId, component.Component, args);
                    ComponentsChanged?.Invoke(this, EventArgs.Empty);
                    break;
                }
            }
        }

        public void DeactivateComponent(string componentName)
        {
            var removed = RemoveActivations(m => m.ComponentName == componentName);

            if (removed > 0)
            {
                ComponentsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void LoadComponentsFromAssembly(Assembly assembly, IPiletService pilet)
        {
            var serviceProvider = _container.ConfigureModule(assembly, pilet);
            var componentTypes = assembly.GetTypesWithAttributes(AttributeTypes);

            foreach (var componentType in componentTypes)
            {
                if (componentType.IsAbstract)
                {
                    // Skip abstract base classes
                    continue;
                }

                var componentNames = GetComponentNamesToRegister(componentType, AttributeTypes);

                foreach (var componentName in componentNames)
                {
                    Register(componentName, componentType, serviceProvider);
                    _logger.LogInformation($"registered {componentName}");
                }
            }
        }

        public void UnloadComponentsFromAssembly(Assembly assembly)
        {
            var componentTypes = assembly.GetTypesWithAttributes(AttributeTypes);

            foreach (var componentType in componentTypes)
            {
                if (componentType.IsAbstract)
                {
                    // Skip abstract base classes
                    continue;
                }

                var componentNames = GetComponentNamesToRegister(componentType, AttributeTypes);

                foreach (var componentName in componentNames)
                {
                    Unregister(componentName);
                    _logger.LogInformation($"unregistered {componentName}");
                }
            }
        }

        private int RemoveActivations(Predicate<ActiveComponent> isActivation)
        {
            var removed = 0;
            var last = _active.Count - 1;

            // Complicated algorithm to still allow Blazor projections to happen, otherwise
            // we'll end up with some dead references if the element to be removed is not
            // at the tail.
            for (var i = last; i >= 0; i--)
            {
                var m = _active[i];

                // Either we find the component already
                if (isActivation(m))
                {
                    // Then let's decide if we can just remove it or mark it as "deprecated" (still giving it a placeholder)
                    if (last == i)
                    {
                        _active.RemoveAt(i);
                        last--;
                    }
                    else
                    {
                        _active[i] = new ActiveComponent(m.ComponentName, m.ReferenceId);
                    }

                    removed++;
                }
                // or we find elements that have been marked, but can now be removed.
                else if (last == i && m.IsDeleted)
                {
                    _active.RemoveAt(i);
                    removed++;
                    last--;
                }
            }

            return removed;
        }

        private Type GetComponent(string componentName)
        {
            _services.TryGetValue(componentName, out var value);
            return value;
        }

        private static IEnumerable<string> GetComponentNamesToRegister(Type member, IEnumerable<Type> attributeTypes)
        {
            return attributeTypes.SelectMany(at => GetComponentNameToRegister(member, at)).Where(val => val != null);
        }

        private static IEnumerable<string> GetComponentNameToRegister(Type member, Type attributeType)
        {
            var attributes = member.GetCustomAttributes(attributeType, true);

            // get only the first occurence of the attribute for all but pages.
            // This is mostly relevant for extensions, which can have multiple attributes,
            // but the name to register (FQN) will be the same for every occurence anyway.
            if (attributeType != typeof(RouteAttribute) && attributes.Count() > 1)
            {
                attributes = attributes.ToList().Take(1).ToArray();
            }

            if (attributes is null)
            {
                return null;
            }

            List<string> result = new();

            foreach (var attribute in attributes)
            {
                result.Add(attributeType switch
                {
                    Type _ when attributeType == typeof(RouteAttribute) =>
                        $"page-{((RouteAttribute)attribute).Template}",
                    Type _ when attributeType == typeof(PiralExtensionAttribute) =>
                        $"extension-{member.FullName}",
                    Type _ when attributeType == typeof(PiralComponentAttribute) =>
                        $"{((PiralComponentAttribute)attribute).Name ?? member.FullName}",
                    Type _ when attributeType == typeof(PiralAppRootAttribute) =>
                        appRoot,
                    _ => null
                });
            }

            return result;
        }
    }
}
