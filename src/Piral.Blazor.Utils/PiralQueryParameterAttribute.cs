using System;
using System.Runtime.CompilerServices;

namespace Piral.Blazor.Utils;

/// <summary>
/// Indicates that routing components my supply a value for the parameter from the
/// current URL querystring. They may also supply further values if the URL querystring changes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class PiralQueryParameterAttribute : Attribute
{
    /// <summary>
    /// Determines the original name of the query parameter name.
    /// </summary>
    public PiralQueryParameterAttribute([CallerMemberName] string propertyName = null)
    {
        QueryParameterName = propertyName;
    }

    /// <summary>
    /// Gets or sets the name of the querystring parameter. Note
    /// that the actual comparison to the query string is case-insensitive.
    /// </summary>
    public string QueryParameterName { get; set; }
}
