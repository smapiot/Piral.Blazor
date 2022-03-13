using Microsoft.Extensions.DependencyInjection;
using System;

namespace Piral.Blazor.Core
{
    /// <summary>
    /// Extends an <see cref="IServiceProvider"/> at runtime.
    /// </summary>
    public interface IPiralServiceProvider : IServiceProvider
    {

        /// <summary>
        /// Adds the pilet's global and local <see cref="IServiceCollection"/>
        /// </summary>
        void AddGlobalServices(IServiceCollection globalServices);

        /// <summary>
        /// Creates a new <see cref="IServiceProvider"/> used by the pilet. 
        /// This <see cref="IServiceProvider"/> will be rebuilt if another module registers global dependencies.
        /// </summary>
        /// <returns>A <see cref="PiletServiceProvider"/></returns>
        PiletServiceProvider CreatePiletServiceProvider(IServiceCollection piletServices);
    }
}