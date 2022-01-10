using System;

namespace Piral.Blazor.Utils
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PiralParameterAttribute : Attribute
    {
        /// <summary>
        /// Registers a Piral extension for a specific extensionId
        /// </summary>
        public PiralParameterAttribute(string jsParameterName)
        {
            JsParameterName = jsParameterName;
        }

        /// <summary>
        /// The extension id provided. This has to correspond to the name of an extension slot.
        /// </summary>
        public string JsParameterName { get; }
    }
}
