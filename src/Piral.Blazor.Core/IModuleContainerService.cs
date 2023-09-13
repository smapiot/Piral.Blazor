using Piral.Blazor.Utils;
using System;
using System.Reflection;

namespace Piral.Blazor.Core;

public interface IModuleContainerService
{
    /// <summary>
    /// Configures the whole assembly (pilet) returning a dedicated service provider.
    /// </summary>
    /// <param name="assembly">The pilet's assembly.</param>
    /// <param name="pilet">The pilet's service.</param>
    void ConfigureModule(Assembly assembly, IPiletService pilet);

    /// <summary>
    /// Gets the provider that was established for the given assembly.
    /// Returns null if no provider has been established.
    /// </summary>
    IServiceProvider GetProvider(Assembly assembly);
}
