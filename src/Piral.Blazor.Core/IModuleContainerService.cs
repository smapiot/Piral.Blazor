using System;
using System.Reflection;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Piral.Blazor.Core
{
    public interface IModuleContainerService
    {
        /// <summary>
        /// Configures the component for rendering.
        /// </summary>
        /// <param name="type">The type to configure.</param>
        /// <param name="provider">The associated provider.</param>
        /// <param name="host">The current WebAssemblyHost.</param>
        void ConfigureComponent(Type type, IServiceProvider provider, WebAssemblyHost host);

        /// <summary>
        /// Removes the component from the render initializer.
        /// </summary>
        /// <param name="type">The type to forget.</param>
        void ForgetComponent(Type type);

        /// <summary>
        /// Configures the whole assembly returning a dedicated service provider.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        IServiceProvider Configure(Assembly assembly);
    }
}
