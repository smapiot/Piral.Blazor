using Microsoft.Extensions.DependencyInjection;
using System;

namespace Piral.Blazor.Core
{
    /// <summary>
    /// Extends an <see cref="IServiceProvider"/> to accept runtime dependencies from pilet
    /// </summary>
    public interface IPiralServiceProvider : IServiceProvider
    {
        /// <summary>
        /// Adds the pilet's <see cref="IServiceCollection"/>
        /// </summary>
        void AddPiletServices(IServiceCollection services);
    }
}