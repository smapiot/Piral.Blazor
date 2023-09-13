using System;

namespace Piral.Blazor.Utils;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class PiralAppRootAttribute : Attribute
{
    /// <summary>
    /// Registers a Piral app root component.
    /// There can only be one.
    /// </summary>
    public PiralAppRootAttribute() { }
}
