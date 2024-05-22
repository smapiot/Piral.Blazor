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
                var desc = ChangeScopedRegistrationToSingleton(service);
                childServices.Add(desc);
                _globalServices.Add(desc);
            }
        });

        return _globalServiceProvider;
    }

    public IServiceProvider CreatePiletServiceProvider(IServiceCollection piletServices)
    {
        return _globalServiceProvider.CreateChildServiceProvider(_globalServices, childServices =>
        {
            foreach (var service in piletServices)
            {
                var desc = ChangeScopedRegistrationToSingleton(service);
                childServices.Add(desc);
            }
        });
    }

    private static ServiceDescriptor ChangeScopedRegistrationToSingleton(ServiceDescriptor item)
    {
        if (item.Lifetime != ServiceLifetime.Scoped || item.IsKeyedService)
        {
            return item;
        }
        else if (item.ImplementationType != null)
        {
            return new ServiceDescriptor(item.ServiceType, item.ImplementationType, ServiceLifetime.Singleton);
        }
        else
        {
            return new ServiceDescriptor(item.ServiceType, item.ImplementationFactory, ServiceLifetime.Singleton);
        }
    }

    public object GetService(Type serviceType) => _globalServiceProvider.GetService(serviceType);
}
