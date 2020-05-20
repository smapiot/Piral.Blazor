using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Piral.Blazor.Core
{
    public static class JSBridge
    {
        public static ComponentActivationService ActivationService { get; set; }

        public static ModuleContainerService ContainerService { get; set; }

        [JSInvokable]
        public static Task LoadComponentsFromLibrary(string data)
        {
            var bytes = Convert.FromBase64String(data);
            var assembly = Assembly.Load(bytes);
            var container = ContainerService?.Configure(assembly);
            ActivationService?.RegisterAll(assembly, container);
            return Task.FromResult(true);
        }

        [JSInvokable]
        public static Task UnloadComponentsFromLibrary(string name)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembly = assemblies.FirstOrDefault(m => m.FullName == name);
            ActivationService?.UnregisterAll(assembly);
            return Task.FromResult(true);
        }

        [JSInvokable]
        public static Task<string> Activate(string componentName, IDictionary<string, object> args)
        {
            var referenceId = Guid.NewGuid().ToString().Split('-').Last();
            ActivationService?.ActivateComponent(componentName, referenceId, args);
            return Task.FromResult(referenceId);
        }

        [JSInvokable]
        public static Task Deactivate(string componentName, string referenceId)
        {
            ActivationService?.DeactivateComponent(componentName, referenceId);
            return Task.FromResult(true);
        }
    }
}
