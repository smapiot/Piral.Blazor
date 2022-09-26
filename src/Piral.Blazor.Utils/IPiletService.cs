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
    }
}
