using Dazinator.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Piral.Blazor.Core
{
    public class PiralServiceProvider : IPiralServiceProvider
    {
        private static readonly IServiceCollection emptyServices = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        private readonly IServiceCollection _globalServices;
        private readonly List<PiletServiceProvider> _piletServiceProviders = new();
        private IServiceProvider _globalServiceProvider;

        public PiralServiceProvider(IServiceCollection globalServices)
        {
            _globalServices = globalServices ?? emptyServices;
            _globalServices.AddSingleton<IPiralServiceProvider>(this);
            _globalServiceProvider = _globalServices.BuildServiceProvider();
        }

        public void AddGlobalServices(IServiceCollection globalServices)
        {
            if (globalServices != null && globalServices.Any())
            {
                _globalServiceProvider = _globalServiceProvider.CreateChildServiceProvider(_globalServices, childServices =>
                {
                    foreach (var service in globalServices)
                    {
                        childServices.Add(service);
                    }
                }, sp => sp.BuildServiceProvider(), ParentSingletonOpenGenericRegistrationsBehaviour.Delegate);

                foreach (var service in globalServices)
                {
                    _globalServices.Add(service);
                }
            }
        }

        public PiletServiceProvider CreatePiletServiceProvider(IServiceCollection piletServices)
        {
            var serviceProvider = new PiletServiceProvider(this, piletServices);
            _piletServiceProviders.Add(serviceProvider);
            return serviceProvider;
        }

        internal static IServiceProvider CreatePiletServiceProvider(
            IServiceProvider globalServiceProvider,
            IServiceCollection piletServices) =>
                globalServiceProvider.CreateChildServiceProvider(emptyServices, childServices =>
                {
                    foreach (var service in piletServices)
                    {
                        childServices.Add(service);
                    }
                }, sp => sp.BuildServiceProvider(), ParentSingletonOpenGenericRegistrationsBehaviour.Delegate);

        public object GetService(Type serviceType) => _globalServiceProvider.GetService(serviceType);
    }
}
