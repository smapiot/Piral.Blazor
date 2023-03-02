using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Piral.Blazor.Core
{
    static class ConfigurationBuildExtensions
    {
        public static void ConfigureLogger(this WebAssemblyHostBuilder builder)
        {
            var section = "Logging";
            var loggingConfig = new LoggingConfiguration();
            builder.Services.AddSingleton<ILoggingConfiguration>(loggingConfig);
            builder.Configuration.AddLoggingConfiguration(loggingConfig, section);
            builder.Logging.AddConfiguration(builder.Configuration.GetSection(section));
        }

        static IConfigurationBuilder AddLoggingConfiguration(this IConfigurationBuilder builder, ILoggingConfigurationProviderCollection configuration, params string[] parentPath)
        {
            return builder.Add(new LoggingConfigurationSource(configuration, parentPath));
        }

        interface ILoggingConfigurationProvider
        {
            void SetLevel(LogLevel level, string category = null, string provider = null);

            void ResetLevel(string category = null, string provider = null);
        }

        interface ILoggingConfigurationProviderCollection
        {
            int Count { get; }

            void Add(ILoggingConfigurationProvider provider);

            void Remove(ILoggingConfigurationProvider provider);
        }

        class LoggingConfigurationProvider : ConfigurationProvider, ILoggingConfigurationProvider
        {
            private readonly IEnumerable<string> _parentPath;

            public LoggingConfigurationProvider(IEnumerable<string> parentPath)
            {
                _parentPath = parentPath ?? throw new ArgumentNullException(nameof(parentPath));

                if (_parentPath.Any(String.IsNullOrWhiteSpace))
                    throw new ArgumentException("The segments of the parent path must be null nor empty.");
            }

            public void SetLevel(LogLevel level, string category = null, string provider = null)
            {
                var path = BuildLogLevelPath(category, provider);
                Data[path] = GetLevelName(level);
                OnReload();
            }

            public void ResetLevel(string category = null, string provider = null)
            {
                if (!String.IsNullOrEmpty(category) || !String.IsNullOrWhiteSpace(provider))
                {
                    var path = BuildLogLevelPath(category, provider);
                    Data.Remove(path);
                }
                else
                {
                    Data.Clear();
                }

                OnReload();
            }

            private static string GetLevelName(LogLevel level)
            {
                return Enum.GetName(typeof(LogLevel), level) ?? throw new ArgumentException($"Provided value is not a valid {nameof(LogLevel)}: {level}", nameof(level));
            }

            private string BuildLogLevelPath(string category, string provider)
            {
                var segments = _parentPath.ToList();

                if (!String.IsNullOrWhiteSpace(provider))
                    segments.Add(provider!.Trim());

                segments.Add("LogLevel");
                segments.Add(String.IsNullOrWhiteSpace(category) ? "Default" : category!.Trim());
                return ConfigurationPath.Combine(segments);
            }
        }

        class LoggingConfigurationSource : IConfigurationSource
        {
            private readonly ILoggingConfigurationProviderCollection _providerCollection;
            private readonly IEnumerable<string> _parentPath;

            public LoggingConfigurationSource(ILoggingConfigurationProviderCollection providerCollection, params string[] parentPath)
            {
                _providerCollection = providerCollection ?? throw new ArgumentNullException(nameof(providerCollection));
                _parentPath = parentPath ?? throw new ArgumentNullException(nameof(parentPath));

                if (_parentPath.Any(String.IsNullOrWhiteSpace))
                    throw new ArgumentException("The segments of the parent path must be null nor empty.");
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                var provider = new LoggingConfigurationProvider(_parentPath);
                _providerCollection.Add(provider);

                return provider;
            }
        }
        class LoggingConfiguration : ILoggingConfiguration, ILoggingConfigurationProviderCollection
        {
            private readonly List<ILoggingConfigurationProvider> _providers;

            public LoggingConfiguration()
            {
                _providers = new List<ILoggingConfigurationProvider>();
            }

            public void SetLevel(LogLevel level, string category = null, string provider = null)
            {
                foreach (var p in _providers)
                {
                    p.SetLevel(level, category, provider);
                }
            }

            public void ResetLevel(string category = null, string provider = null)
            {
                foreach (var p in _providers)
                {
                    p.ResetLevel(category, provider);
                }
            }

            int ILoggingConfigurationProviderCollection.Count => _providers.Count;

            void ILoggingConfigurationProviderCollection.Add(ILoggingConfigurationProvider provider)
            {
                if (provider == null)
                    throw new ArgumentNullException(nameof(provider));

                _providers.Add(provider);
            }

            void ILoggingConfigurationProviderCollection.Remove(ILoggingConfigurationProvider provider)
            {
                if (provider == null)
                    throw new ArgumentNullException(nameof(provider));

                _providers.Remove(provider);
            }
        }
    }
}
