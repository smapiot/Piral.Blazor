using Microsoft.Extensions.DependencyInjection;
using System;

namespace Piral.Blazor.Core.Dependencies;

public class PiralServiceProvider : IPiralServiceProvider
{
    private readonly IServiceCollection _globalServices;
    private IServiceProvider _globalServiceProvider;

    public PiralServiceProvider(IServiceCollection globalServices)
    {
        _globalServices = globalServices ?? new ServiceCollection();
        _globalServices.AddSingleton<IPiralServiceProvider>(this);
        _globalServiceProvider = _globalServices.BuildServiceProvider();
    }

    public IServiceProvider ExtendGlobalServiceProvider(IServiceCollection piletServices)
    {
        _globalServiceProvider = _globalServiceProvider.CreateChildServiceProvider(_globalServices, childServices =>
        {
            foreach (var service in piletServices)
            {
                if (service.Lifetime != ServiceLifetime.Scoped || service.IsKeyedService)
                {
                    childServices.Add(service);
                }
                else if (service.ImplementationType != null)
                {
                    childServices.Add(new ServiceDescriptor(service.ServiceType, service.ImplementationType, ServiceLifetime.Singleton));
                }
                else
                {
                    childServices.Add(new ServiceDescriptor(service.ServiceType, service.ImplementationFactory, ServiceLifetime.Singleton));
                }
            }
        });

        foreach (var service in piletServices)
        {
            _globalServices.Add(service);
        }

        return _globalServiceProvider;
    }

    public IServiceProvider CreatePiletServiceProvider(IServiceCollection piletServices)
    {
        return _globalServiceProvider.CreateChildServiceProvider(_globalServices, childServices =>
        {
            foreach (var service in piletServices)
            {
                childServices.Add(service);
            }
        });
    }

    public object GetService(Type serviceType) => _globalServiceProvider.GetService(serviceType);
}
