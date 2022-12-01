using System;

namespace Piral.Blazor.Utils
{
    public sealed class PiralAppRootAttribute : Attribute
    {
        /// <summary>
        /// Registers a Piral app root component.
        /// There can only be one.
        /// </summary>
        public PiralAppRootAttribute() { }
    }
}
