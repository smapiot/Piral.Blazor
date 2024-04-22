using System;

namespace Piral.Blazor.Utils;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class PiralProviderAttribute : Attribute
{
    /// <summary>
    /// Registers a Piral provider component.
    /// </summary>
    public PiralProviderAttribute() { }
}
