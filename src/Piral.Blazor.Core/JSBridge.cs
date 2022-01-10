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
        public static async Task LoadComponentsFromLibrary(string url)
        {
            var dll = await _client.GetStreamAsync(url);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(dll);
            ActivationService?.LoadComponentsFromAssembly(assembly, _host);
        }

        [JSInvokable]
        public static async Task LoadComponentsWithSymbolsFromLibrary(string dllUrl, string pdbUrl)
        {
            var dll = await _client.GetStreamAsync(dllUrl);
            var pdb = await _client.GetStreamAsync(pdbUrl);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(dll, pdb);
            ActivationService?.LoadComponentsFromAssembly(assembly, _host);
        }

        /// <summary>Every series of characters that is not alphanumeric gets consolidated into a dash</summary>
        private static string Sanitize(string value) => Regex.Replace(value, @"[^a-zA-Z0-9]+", "-");
    }
}
