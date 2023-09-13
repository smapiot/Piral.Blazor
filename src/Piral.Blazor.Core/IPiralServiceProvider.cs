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
    /// This <see cref="IServiceProvider"/> will be rebuilt if another module registers global dependencies.
    /// </summary>
    /// <returns>A child service provider.</returns>
    IServiceProvider CreatePiletServiceProvider(IServiceCollection piletServices);
}