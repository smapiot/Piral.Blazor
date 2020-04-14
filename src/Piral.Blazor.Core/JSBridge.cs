using Microsoft.JSInterop;
using Piral.Blazor.Utils;
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

        [JSInvokable]
        public static Task LoadComponentsFromLibrary(string data)
        {
            var bytes = Convert.FromBase64String(data);
            var assembly = Assembly.Load(bytes);
            var types = assembly.GetTypes().Where(m => m.GetCustomAttribute<ExposePiletAttribute>(false) != null);

            foreach (var type in types)
            {
                var name = type.GetCustomAttribute<ExposePiletAttribute>(false).Name;
                ActivationService?.Register(name, type);
            }

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
