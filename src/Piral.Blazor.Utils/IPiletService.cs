using System;
using Microsoft.Extensions.Configuration;

namespace Piral.Blazor.Utils
{
    /// <summary>
    /// Represents a set of helper functions to be used in Piral Blazor components.
    /// </summary>
    public interface IPiletService
    {
        /// <summary>
        /// Converts a given local URL (e.g., /images/foo.png) to a URL
        /// within the currently running pilet (e.g.,
        /// https://current.cdn.com/pilets/my-pilet/1.0.0/images/foo.png).
        /// </summary>
        string GetUrl(string localPath);

        /// <summary>
        /// Gets the configuration object for the current pilet.
        /// </summary>
        IConfiguration Config { get; }

        /// <summary>
        /// Gets the name of the current pilet.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the version of the current pilet.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Event emitted when the language has been changed.
        /// </summary>
        event EventHandler LanguageChanged;
    }
}
