using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Piral.Blazor.Core
{
    public class PiralServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        public IServiceCollection CreateBuilder(IServiceCollection services) => services;

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            var provider = new PiralServiceProvider();
            provider.AddProvider(containerBuilder.BuildServiceProvider());
            return provider;
        }

        class PiralServiceProvider : IGlobalServiceProvider
        {
            private readonly List<IServiceProvider> _providers = new List<IServiceProvider>();

            public void AddProvider(IServiceProvider provider)
            {
                _providers.Add(provider);
            }

            public object GetService(Type serviceType)
            {
                foreach (var provider in _providers)
                {
                    var instance = provider.GetService(serviceType);

                    if (instance is not null)
                    {
                        return instance;
                    }
                }

                return null;
            }
        }
    }
}
