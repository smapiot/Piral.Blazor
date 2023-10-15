using Microsoft.Extensions.DependencyInjection;
using System;

namespace Piral.Blazor.Core;

/// <summary>
/// Extends an <see cref="IServiceProvider"/> at runtime.
/// </summary>
public interface IPiralServiceProvider : IServiceProvider
{
    /// <summary>
    /// Creates a new <see cref="IServiceProvider"/> used by the pilet. 
    /// </summary>
    /// <returns>A child service provider.</returns>
    IServiceProvider CreatePiletServiceProvider(IServiceCollection piletServices);
    
    /// <summary>
    /// Creates a new <see cref="IServiceProvider"/> by extending the previous one. 
    /// </summary>
    /// <returns>A global service provider.</returns>
    IServiceProvider ExtendGlobalServiceProvider(IServiceCollection piletServices);
}