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
                childServices.Add(service);
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
