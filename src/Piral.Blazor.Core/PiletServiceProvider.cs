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
        /// <param name="globalServices">A collection of global service registrations.</param>
        /// <param name="piletServices">A collection of pilet specific service registrations.</param>
        public PiletServiceProvider(IServiceProvider globalProvider, IServiceCollection globalServices, IServiceCollection piletServices)
        {
            _piletServices = piletServices;
            _globalProvider = globalProvider;
            _piletProvider = PiralServiceProvider.CreatePiletServiceProvider(globalProvider, globalServices, piletServices);
        }

        /// <summary>
        /// Adds newly registered global dependencies to this <see cref="PiletServiceProvider"/>.
        /// </summary>
        /// <param name="globalProvider">The updated global service provider</param>
        /// <param name="globalServices">The updated global service registrations</param>
        public void Update(IServiceProvider globalProvider, IServiceCollection globalServices)
        {
            _globalProvider = globalProvider;
            _piletProvider = PiralServiceProvider.CreatePiletServiceProvider(globalProvider, globalServices, _piletServices);
        }

        public object GetService(Type serviceType)
        {
            try
            {
                var result = _piletProvider.GetService(serviceType);

                // the result might be null, or in some cases (e.g., ILogger) an InvalidOperationException
                if (result is null)
                {
                    throw new InvalidOperationException($"Unable to resolve service for type '{serviceType}'.");
                }

                // we got something, so just return it
                return result;
            }
            catch (InvalidOperationException)
            {
                // just use whatever the global provider does with it
                return _globalProvider.GetService(serviceType);
            }
        }
    }
}
