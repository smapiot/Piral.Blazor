using System;

namespace Piral.Blazor.Utils
{
    public sealed class PiralComponentAttribute : Attribute
    {
        /// <summary>
        /// Registers a Piral component
        /// </summary>
        public PiralComponentAttribute() { }

        /// <summary>
        /// Registers a Piral component using a custom name
        /// </summary>
        public PiralComponentAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the component in the pilet.
        /// </summary>
        public string Name { get; }
    }
}
