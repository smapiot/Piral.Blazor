using System;

namespace Piral.Blazor.Utils
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PiralParameterAttribute : Attribute
    {
        /// <summary>
        /// Determines the original name of the parameter in JS code.
        /// Alternatively, allows you to specify sub-queries, such as "a.b.c", which
        /// may be relevant to avoid creating temporary classes.
        /// </summary>
        public PiralParameterAttribute(string jsParameterName)
        {
            JsParameterName = jsParameterName;
        }

        /// <summary>
        /// The original parameter name of the JS object.
        /// </summary>
        public string JsParameterName { get; }
    }
}
