using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            Localization.Language = language;
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task ProcessEvent(string type, JsonElement args)
        {
            foreach (var pilet in _pilets.Values)
            {
                pilet.Service.CallEventListeners(type, args);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Modern Loading / Initialization API

        [JSInvokable]
        public static Task<string[]> GetCapabilities()
        {
            // "load" --> enables using "LoadPilet" / "UnloadPilet" instead of "LoadComponentsFromLibrary" etc.
            // "custom-element" --> enables using "CreateElement" etc. intead of "Activate" etc.
            // "language" --> enables using satellite assemblies
            // "logging" --> enables setting the log level
            // "events" --> enables support for emitting / subscribing to Piral events
            // "dependency-symbols" --> enables support for dependency symbols in the metadata
            return Task.FromResult(new[] { "load", "custom-element", "language", "logging", "events", "dependency-symbols" });
        }

        [JSInvokable]
        public static async Task LoadPilet(string id, PiletDefinition pilet)
        {
            var data = new PiletData { };

            if (_pilets.TryAdd(id, data))
            {
                var client = Host.Services.GetRequiredService<HttpClient>();
                var js = Host.Services.GetService<IJSRuntime>();
                var context = new AssemblyLoadContext(id, true);

                foreach (var url in pilet.Dependencies)
                {
                    var name = url.Split('/').Last();
                    var symbols = string.Concat(url.AsSpan(0, url.Length - 4), ".pdb");
                    var pdbUrl = (pilet.DependencySymbols?.Contains(symbols) ?? false) ? symbols : null;
                    await LoadAssemblyInContext(client, context, url, pdbUrl);
                }

                var library = await LoadAssemblyInContext(client, context, pilet.DllUrl, pilet.PdbUrl);
                var service = new PiletService(js, client, pilet);
                data.Library = library;
                data.Service = service;
                data.Context = context;

                data.LanguageHandler = async (s, e) =>
                {
                    await data.Service.LoadLanguage(Localization.Language);
                    service.InformLanguageChange();
                };

                Localization.LanguageChanged += data.LanguageHandler;
                ActivationService?.LoadComponentsFromAssembly(data.Library, data.Service);
            }
        }

        [JSInvokable]
        public static Task UnloadPilet(string id)
        {
            if (_pilets.Remove(id, out var data))
            {
                Localization.LanguageChanged -= data.LanguageHandler;
                ActivationService?.UnloadComponentsFromAssembly(data.Library);
                data.Context.Unload();
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Logging API

        [JSInvokable]
        public static Task SetLogLevel(int level)
        {
            var config = Host.Services.GetService<ILoggingConfiguration>();
            config.SetLevel((LogLevel)level);
            return Task.CompletedTask;
        }

        #endregion

        #region Legacy Loading API

        [JSInvokable]
        public static async Task LoadComponentsFromLibrary(string url)
        {
            var client = Host.Services.GetRequiredService<HttpClient>();
            var js = Host.Services.GetService<IJSRuntime>();
            var dll = await client.GetStreamAsync(url);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(dll);
            var pilet = new PiletService(js, client, url);
            ActivationService?.LoadComponentsFromAssembly(assembly, pilet);
            _assemblies[url] = assembly;
        }

        [JSInvokable]
        public static async Task LoadComponentsWithSymbolsFromLibrary(string dllUrl, string pdbUrl)
        {
            var client = Host.Services.GetRequiredService<HttpClient>();
            var js = Host.Services.GetService<IJSRuntime>();
            var dll = await client.GetStreamAsync(dllUrl);
            var pdb = await client.GetStreamAsync(pdbUrl);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(dll, pdb);
            var pilet = new PiletService(js, client, dllUrl);
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

        private static async Task<Assembly> LoadAssemblyInContext(HttpClient client, AssemblyLoadContext context, string dllUrl, string pdbUrl)
        {
            if (pdbUrl is not null)
            {
                var streams = await Task.WhenAll(client.GetStreamAsync(dllUrl), client.GetStreamAsync(pdbUrl));
                var dll = streams[0];
                var pdb = streams[1];
                return context.LoadFromStream(dll, pdb);
            }
            else
            {
                var dll = await client.GetStreamAsync(dllUrl);
                return context.LoadFromStream(dll);
            }
        }

        class PiletData
        {
            public Assembly Library { get; set; }

            public PiletService Service { get; set; }

            public EventHandler LanguageHandler { get; set; }

            public AssemblyLoadContext Context { get; set; }
        }

        #endregion
    }
}
