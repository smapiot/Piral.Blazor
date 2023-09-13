using Microsoft.Extensions.DependencyInjection;
using System;

namespace Piral.Blazor.Core.Dependencies;

internal class PiralServiceProvider : IPiralServiceProvider
{
    private readonly IServiceCollection _globalServices;
    private readonly IServiceProvider _globalServiceProvider;

    public PiralServiceProvider(IServiceCollection globalServices)
    {
        _globalServices = globalServices ?? new ServiceCollection();
        _globalServices.AddSingleton<IPiralServiceProvider>(this);
        _globalServiceProvider = _globalServices.BuildServiceProvider();
    }

    public IServiceProvider CreatePiletServiceProvider(IServiceCollection piletServices)
    {
        return _globalServiceProvider.CreateChildServiceProvider(_globalServices, childServices =>
        {
            foreach (var service in piletServices)
            {
                childServices.Add(service);
            }
        }, sp => sp.BuildServiceProvider(), ParentSingletonOpenGenericRegistrationsBehaviour.Delegate);
    }

    public object GetService(Type serviceType) => _globalServiceProvider.GetService(serviceType);
}
