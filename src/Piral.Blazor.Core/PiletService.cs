using Piral.Blazor.Utils;
using System;

namespace Piral.Blazor.Core
{
    public sealed class PiletService : IPiletService
    {
        private readonly Uri _baseUrl;

        public PiletService(string baseUrl)
        {
            // Move base URL up in case of a `_framework` containment
            _baseUrl = new Uri(baseUrl.Replace("/_framework/", "/"));
        }

        public string GetUrl(string localPath)
        {
            if (localPath.StartsWith("/") && !localPath.StartsWith("//"))
            {
                localPath = localPath.Substring(1);
            }

            return new Uri(_baseUrl, localPath).AbsoluteUri;
        }
    }
}
