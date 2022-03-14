using Microsoft.Extensions.DependencyInjection;
using System;

namespace Piral.Blazor.Core
{
    public class PiralServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        public IServiceCollection CreateBuilder(IServiceCollection services) => services;

        public IServiceProvider CreateServiceProvider(IServiceCollection services) => new PiralServiceProvider(services);
    }
}
