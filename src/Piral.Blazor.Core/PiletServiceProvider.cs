using Microsoft.Extensions.DependencyInjection;
using System;

namespace Piral.Blazor.Core
{
    /// <summary>
    /// Each pilet has its own <see cref="IServiceProvider"/> that can resolve global and pilet specific dependencies.
    /// If a pilet registers global services all already established <see cref="PiletServiceProvider"/> instances have to be rebuilt.
    /// </summary>
    public class PiletServiceProvider : IServiceProvider
    {
        private readonly IServiceCollection _piletServices;
        private IServiceProvider _globalProvider;
        private IServiceProvider _piletProvider;

        /// <summary>
        /// Constructs a new <see cref="PiletServiceProvider"/>.
        /// </summary>
        /// <param name="globalProvider">The current global service provider </param>
        /// <param name="piletServices">A collection of pilet specific service registrations.</param>
        public PiletServiceProvider(IServiceProvider globalProvider, IServiceCollection piletServices)
        {
            _piletServices = piletServices;
            _globalProvider = globalProvider;
            _piletProvider = PiralServiceProvider.CreatePiletServiceProvider(globalProvider, piletServices);
        }

        public object GetService(Type serviceType) => _piletProvider.GetService(serviceType) ?? _globalProvider.GetService(serviceType);
    }
}
