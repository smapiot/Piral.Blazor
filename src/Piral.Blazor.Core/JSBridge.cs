using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Piral.Blazor.Core
{
    public static class JSBridge
    {
        public static ComponentActivationService ActivationService { get; set; }

        private static HttpClient _client;
        private static WebAssemblyHost _host;

        public static void Configure(HttpClient client, WebAssemblyHost host)
        {
            _client = client;
            _host = host;
        }

        [JSInvokable]
        public static Task<string> Activate(string componentName, IDictionary<string, JsonElement> args)
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
        public static Task Reactivate(string componentName, string referenceId, IDictionary<string, JsonElement> args)
        {
            ActivationService?.ReactivateComponent(componentName, referenceId, args);
            return Task.FromResult(true);
        }

        private static Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>();

        [JSInvokable]
        public static async Task LoadComponentsFromLibrary(string url)
        {
            var dll = await _client.GetStreamAsync(url);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(dll);
            ActivationService?.LoadComponentsFromAssembly(assembly, _host);
            _assemblies[url] = assembly;
        }

        [JSInvokable]
        public static async Task LoadComponentsWithSymbolsFromLibrary(string dllUrl, string pdbUrl)
        {
            var dll = await _client.GetStreamAsync(dllUrl);
            var pdb = await _client.GetStreamAsync(pdbUrl);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(dll, pdb);
            ActivationService?.LoadComponentsFromAssembly(assembly, _host);
            _assemblies[dllUrl] = assembly;
        }

        [JSInvokable]
        public static async Task UnloadComponentsFromLibrary(string url)
        {
            if (_assemblies.TryGetValue(url, out var assembly))
            {
                ActivationService?.UnloadComponentsFromAssembly(assembly);
            }
        }

        /// <summary>Every series of characters that is not alphanumeric gets consolidated into a dash</summary>
        private static string Sanitize(string value) => Regex.Replace(value, @"[^a-zA-Z0-9]+", "-");
    }
}
