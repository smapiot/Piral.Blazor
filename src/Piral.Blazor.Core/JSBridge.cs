using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Piral.Blazor.Core
{
    public static class JSBridge
    {
        private static Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>();
        private static Dictionary<string, PiletData> _pilets = new Dictionary<string, PiletData>();

        public static ComponentActivationService ActivationService { get; set; }

        public static WebAssemblyHost Host { get; set; }

        public static void Initialize(WebAssemblyHost host)
        {
            Host = host;
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
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task Reactivate(string componentName, string referenceId, IDictionary<string, JsonElement> args)
        {
            ActivationService?.ReactivateComponent(componentName, referenceId, args);
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task<string[]> GetCapabilities()
        {
            return Task.FromResult(new[] { "load" });
        }

        [JSInvokable]
        public static async Task LoadPilet(string id, PiletDefinition pilet)
        {
            var data = new PiletData { };

            if (_pilets.TryAdd(id, data))
            {
                var client = Host.Services.GetRequiredService<HttpClient>();
                var dll = await client.GetStreamAsync(pilet.DllUrl);
                var pdb = pilet.PdbUrl != null ? await client.GetStreamAsync(pilet.PdbUrl) : null;
                data.Library = AssemblyLoadContext.Default.LoadFromStream(dll, pdb);
                data.Service = new PiletService(pilet);

                foreach (var url in pilet.Dependencies)
                {
                    var dep = await client.GetStreamAsync(url);
                    AssemblyLoadContext.Default.LoadFromStream(dep);
                }

                ActivationService?.LoadComponentsFromAssembly(data.Library, data.Service);
            }
        }

        [JSInvokable]
        public static Task UnloadPilet(string id)
        {
            if (_pilets.Remove(id, out var data))
            {
                ActivationService?.UnloadComponentsFromAssembly(data.Library);
            }

            return Task.CompletedTask;
        }

        [JSInvokable]
        public static async Task LoadComponentsFromLibrary(string url)
        {
            var client = Host.Services.GetRequiredService<HttpClient>();
            var dll = await client.GetStreamAsync(url);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(dll);
            var pilet = new PiletService(url);
            ActivationService?.LoadComponentsFromAssembly(assembly, pilet);
            _assemblies[url] = assembly;
        }

        [JSInvokable]
        public static async Task LoadComponentsWithSymbolsFromLibrary(string dllUrl, string pdbUrl)
        {
            var client = Host.Services.GetRequiredService<HttpClient>();
            var dll = await client.GetStreamAsync(dllUrl);
            var pdb = await client.GetStreamAsync(pdbUrl);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(dll, pdb);
            var pilet = new PiletService(dllUrl);
            ActivationService?.LoadComponentsFromAssembly(assembly, pilet);
            _assemblies[dllUrl] = assembly;
        }

        [JSInvokable]
        public static Task UnloadComponentsFromLibrary(string url)
        {
            if (_assemblies.TryGetValue(url, out var assembly))
            {
                ActivationService?.UnloadComponentsFromAssembly(assembly);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Every series of characters that is not alphanumeric gets consolidated into a dash
        /// </summary>
        private static string Sanitize(string value) => Regex.Replace(value, @"[^a-zA-Z0-9]+", "-");

        class PiletData
        {
            public Assembly Library { get; set; }

            public IPiletService Service { get; set; }
        }
    }
}
