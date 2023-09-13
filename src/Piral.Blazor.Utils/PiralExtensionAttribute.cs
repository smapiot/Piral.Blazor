using System;

namespace Piral.Blazor.Utils;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PiralExtensionAttribute : Attribute
{
    /// <summary>
    /// Registers a Piral extension for a specific extensionId
    /// </summary>
    public PiralExtensionAttribute(string extensionId)
    {
        ExtensionId = extensionId;
    }

    /// <summary>
    /// The extension id provided. This has to correspond to the name of an extension slot.
    /// </summary>
    public string ExtensionId { get; }
}
