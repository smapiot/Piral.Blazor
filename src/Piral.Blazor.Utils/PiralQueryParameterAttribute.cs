using System;

namespace Piral.Blazor.Utils;

/// <summary>
/// Indicates that routing components my supply a value for the parameter from the
/// current URL querystring. They may also supply further values if the URL querystring changes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class PiralQueryParameterAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the querystring parameter. If null, the querystring
    /// parameter is assumed to have the same name as associated property.
    /// </summary>
    public string Name { get; set; }
}
