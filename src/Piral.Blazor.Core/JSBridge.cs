using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Piral.Blazor.Core
{
    public static class JSBridge
    {
        public static ComponentActivationService ActivationService { get; set; }

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
