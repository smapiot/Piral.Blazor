using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Piral.Blazor.Utils;

namespace Piral.Blazor.Core
{
    public sealed class PiletService : IPiletService
    {
        private readonly Uri _baseUrl;
        private readonly IConfiguration _config;
        private readonly string _name;
        private readonly string _version;

        public PiletService(string baseUrl)
        {
            _name = "(unknown)";
            _version = "0.0.0";

            // Move base URL up in case of a `_framework` containment
            _baseUrl = new Uri(baseUrl.Replace("/_framework/", "/"));
            // Create config wrapper
            _config = new ConfigurationBuilder().AddJsonStream(GetStream("{}")).Build();
        }

        public PiletService(PiletDefinition pilet)
        {
            _name = pilet.Name;
            _version = pilet.Version;

            // Move base URL up in case of a `_framework` containment
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

        private static Stream GetStream(string s) => new MemoryStream(Encoding.UTF8.GetBytes(s));
    }
}
