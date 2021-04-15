using System;

namespace Piral.Blazor.Utils
{
    [Obsolete("Please use the PiralComponent or PiralExtension attributes.")]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExposePiletAttribute : Attribute
    {
        public ExposePiletAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the component in the pilet.
        /// </summary>
        public string Name { get; }
    }
}
