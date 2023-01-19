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
        private static HashSet<string> _dependencies = new HashSet<string>();
        private static Dictionary<string, PiletData> _pilets = new Dictionary<string, PiletData>();

        public static ComponentActivationService ActivationService { get; set; }

        public static WebAssemblyHost Host { get; set; }

        public static void Initialize(WebAssemblyHost host)
        {
            Host = host;
        }

        #region Custom Element API

        [JSInvokable]
        public static Task<string> CreateElement(string componentName, IDictionary<string, JsonElement> args)
        {
            var referenceId = Guid.NewGuid().ToString();
            ActivationService?.MountComponent(componentName, referenceId, args);
            return Task.FromResult(referenceId);
        }

        [JSInvokable]
        public static Task UpdateElement(string referenceId, IDictionary<string, JsonElement> args)
        {
            ActivationService?.UpdateComponent(referenceId, args);
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task DestroyElement(string referenceId)
        {
            ActivationService?.UnmountComponent(referenceId);
            return Task.CompletedTask;
        }

        #endregion
        
        #region Legacy Rendering (w. projection) API

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

        #endregion

        #region Auxiliary APIs

        [JSInvokable]
        public static Task SetLanguage(string language)
        {
            if (ActivationService is not null)
            {
                ActivationService.Language = language;
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Modern Loading / Initialization API

        [JSInvokable]
        public static Task<string[]> GetCapabilities()
        {
            // "load" --> enables using "LoadPilet" / "UnloadPilet" instead of "LoadComponentsFromLibrary" etc.
            // "language" --> enables using 
            return Task.FromResult(new[] { "load", "language" });
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
                var library = AssemblyLoadContext.Default.LoadFromStream(dll, pdb);
                var service = new PiletService(pilet);
                data.Library = library;
                data.Service = service;

                foreach (var url in pilet.Dependencies)
                {
                    var name = url.Split('/').Last();

                    if (_dependencies.Add(name))
                    {
                        var dep = await client.GetStreamAsync(url);
                        AssemblyLoadContext.Default.LoadFromStream(dep);
                    }
                }

                if (pilet.Satellites is not null && ActivationService is not null)
                {
                    Func<string, Task> changeLanguage = async (language) =>
                    {
                        if (pilet.Satellites.TryGetValue(language, out var satellites))
                        {
                            foreach (var satellite in satellites)
                            {
                                var url = data.Service.GetUrl(satellite);
                                var dep = await client.GetStreamAsync(url);
                                AssemblyLoadContext.Default.LoadFromStream(dep);
                            }
                        }

                        service.InformLanguageChange();
                    };

                    data.LanguageHandler = (s, e) =>
                    {
                        changeLanguage(ActivationService.Language);
                    };

                    ActivationService.LanguageChanged += data.LanguageHandler;
                    data.LanguageHandler.Invoke(null, EventArgs.Empty);
                }

                ActivationService?.LoadComponentsFromAssembly(data.Library, data.Service);
            }
        }

        [JSInvokable]
        public static Task UnloadPilet(string id)
        {
            if (_pilets.Remove(id, out var data) && ActivationService is not null)
            {
                ActivationService.LanguageChanged -= data.LanguageHandler;
                ActivationService.UnloadComponentsFromAssembly(data.Library);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Legacy Loading API

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

        #endregion

        #region Helpers

        /// <summary>
        /// Every series of characters that is not alphanumeric gets consolidated into a dash
        /// </summary>
        private static string Sanitize(string value) => Regex.Replace(value, @"[^a-zA-Z0-9]+", "-");

        class PiletData
        {
            public Assembly Library { get; set; }

            public PiletService Service { get; set; }

            public EventHandler LanguageHandler { get; set; }
        }

        #endregion
    }
}
