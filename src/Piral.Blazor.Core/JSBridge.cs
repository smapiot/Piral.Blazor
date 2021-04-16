using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
            var guidSegment = Guid.NewGuid().ToString().Split('-').Last();
            var referenceId = $"piral-blazor-{Sanitize(componentName)}-{guidSegment}";
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
            var dll = await _client.GetByteArrayAsync(dllUrl);
            var pdb = await _client.GetByteArrayAsync(pdbUrl);
            var assembly = Assembly.Load(dll, pdb);
            ActivationService?.LoadComponentsFromAssembly(assembly);
        }
        
        /// <summary>Every series of characters that is not alphanumeric gets consolidated into a dash</summary>
        private static string Sanitize(string value)
        {
            return Regex.Replace(value, @"[^a-zA-Z0-9]+", "-");
        }
    }
}
