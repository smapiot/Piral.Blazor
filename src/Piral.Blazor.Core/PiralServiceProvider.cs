using Dazinator.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Piral.Blazor.Core
{
    public class PiralServiceProvider : IPiralServiceProvider
    {
        private readonly IServiceCollection _gloabalServices;
        private readonly IServiceProvider _gloabalServiceProvider;

        private IServiceProvider _piletServiceProvider;

        public PiralServiceProvider(IServiceCollection globalServices)
        {
            _gloabalServices = globalServices ?? new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            _gloabalServices.AddSingleton<IPiralServiceProvider>(this);
            _gloabalServiceProvider = _gloabalServices.BuildServiceProvider();
        }

        public void AddPiletServices(IServiceCollection piletServices)
        {
            _piletServiceProvider = _gloabalServiceProvider.CreateChildServiceProvider(_gloabalServices, childServices =>
            {
                foreach (var service in piletServices)
                {
                    childServices.Add(service);
                }
            }, sp => sp.BuildServiceProvider(), ParentSingletonOpenGenericRegistrationsBehaviour.Delegate);
        }

        public object GetService(Type serviceType)
        {
            var service = _piletServiceProvider?.GetService(serviceType);
            if (service == null)
            {
                service = _gloabalServiceProvider.GetService(serviceType);
            }
            return service;
        }
    }
}
