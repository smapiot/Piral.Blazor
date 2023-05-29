using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Piral.Blazor.Utils;

namespace Piral.Blazor.Core
{
    public sealed class PiletService : IPiletService
    {
        private readonly Uri _baseUrl;
        private readonly IConfiguration _config;
        private readonly string _name;
        private readonly string _version;
        private readonly IJSRuntime _js;
        private readonly List<EventListener> _listeners;

        public event EventHandler LanguageChanged;

        private PiletService(IJSRuntime js)
        {
            _js = js;
            _listeners = new List<EventListener>();
        }

        public PiletService(IJSRuntime js, string baseUrl) : this(js)
        {
            _name = "(unknown)";
            _version = "0.0.0";

            // Move base URL up in case of a `_framework` containment
            _baseUrl = new Uri(baseUrl.Replace("/_framework/", "/"));
            // Create config wrapper
            _config = new ConfigurationBuilder().AddJsonStream(GetStream("{}")).Build();
        }

        public PiletService(IJSRuntime js, PiletDefinition pilet) : this(js)
        {
            _name = pilet.Name;
            _version = pilet.Version;

            // base URL is already given
            _baseUrl = new Uri(pilet.BaseUrl);
            // Create config wrapper
            _config = new ConfigurationBuilder().AddJsonStream(GetStream(pilet.Config)).Build();
        }

        public string Name => _name;

        public string Version => _version;

        public IConfiguration Config => _config;

        public string GetUrl(string localPath)
        {
            if (localPath.StartsWith("/") && !localPath.StartsWith("//"))
            {
                localPath = localPath.Substring(1);
            }

            return new Uri(_baseUrl, localPath).AbsoluteUri;
        }

        public void InformLanguageChange() => LanguageChanged?.Invoke(this, EventArgs.Empty);

        public void DispatchEvent<T>(string type, T args)
        {
            _js.InvokeVoidAsync("Blazor.emitPiralEvent", type, args);
        }

        public void AddEventListener<T>(string type, Action<T> handler)
        {
            _listeners.Add(
                new EventListener
                {
                    Handler = (json) =>
                    {
                        handler.Invoke(json.Deserialize<T>());
                    },
                    Reference = handler,
                    Type = type,
                }
            );
        }

        public void RemoveEventListener<T>(string type, Action<T> handler)
        {
            _listeners.RemoveAll(listener => listener.Type == type && Object.ReferenceEquals(listener.Reference, handler));
        }

        public void CallEventListeners(string type, JsonElement args)
        {
            foreach (var listener in _listeners)
            {
                if (listener.Type == type)
                {
                    listener.Handler.Invoke(args);
                }
            }
        }

        private static Stream GetStream(string s) => new MemoryStream(Encoding.UTF8.GetBytes(s));

        struct EventListener
        {
            public object Reference;
            public string Type;
            public Action<JsonElement> Handler;
        }
    }
}
