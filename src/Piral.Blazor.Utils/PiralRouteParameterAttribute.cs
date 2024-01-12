using System;
using System.Runtime.CompilerServices;

namespace Piral.Blazor.Utils;

/// <summary>
/// Indicates that routing components my supply a value for the parameter from the
/// current URL path. They may also supply further values if the URL path changes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class PiralRouteParameterAttribute : PiralParameterAttribute
{
    /// <summary>
    /// Determines the original name of the route parameter in JS code.
    /// Alternatively, allows you to specify sub-queries, such as "a.b.c", which
    /// may be relevant to avoid creating temporary classes.
    /// </summary>
    public PiralRouteParameterAttribute([CallerMemberName] string propertyName = null)
        : base($"match.params.{propertyName}")
    {
    }
}
