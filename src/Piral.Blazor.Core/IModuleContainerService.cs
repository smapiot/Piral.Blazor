using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Piral.Blazor.Utils;
using System;
using System.Reflection;

namespace Piral.Blazor.Core
{
    public interface IModuleContainerService
    {
        /// <summary>
        /// Configures the host for rendering.
        /// </summary>
        /// <param name="host">The current WebAssemblyHost.</param>
        void ConfigureHost(WebAssemblyHost host);

        /// <summary>
        /// Configures the component for rendering.
        /// </summary>
        /// <param name="type">The type to configure.</param>
        /// <param name="provider">The associated provider.</param>
        void ConfigureComponent(Type type, IServiceProvider provider);

        /// <summary>
        /// Removes the component from the render initializer.
        /// </summary>
        /// <param name="type">The type to forget.</param>
        void ForgetComponent(Type type);

        /// <summary>
        /// Configures the whole assembly (pilet) returning a dedicated service provider.
        /// </summary>
        /// <param name="assembly">The pilet's assembly.</param>
        /// <param name="pilet">The pilet's service.</param>
        /// <returns>The service provider for the pilet.</returns>
        IServiceProvider ConfigureModule(Assembly assembly, IPiletService pilet);
    }
}
