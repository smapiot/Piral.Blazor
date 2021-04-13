using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Piral.Blazor.Core
{
    public static class JSBridge
    {
        public static ComponentActivationService ActivationService { get; set; }

        private static HttpClient _client;

        public static void Configure(HttpClient client)
        {
            _client = client;
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

        [JSInvokable]
        public static async Task LoadComponentsFromLibrary(string url)
        {
            var data = await _client.GetByteArrayAsync(url);
            var assembly = Assembly.Load(data);
            ActivationService?.LoadComponentsFromAssembly(assembly);
        }

        [JSInvokable]
        public static async Task LoadComponentsWithSymbolsFromLibrary(string dllUrl, string pdbUrl)
        {
            var task1 = _client.GetByteArrayAsync(dllUrl);
            var task2 = _client.GetByteArrayAsync(pdbUrl);
            await Task.WhenAll(task1, task2);
            var assembly = Assembly.Load(task1.Result, task2.Result);
            ActivationService?.LoadComponentsFromAssembly(assembly);
        }
    }
}
