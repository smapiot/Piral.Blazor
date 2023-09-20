using System;
using System.Threading.Tasks;

namespace Piral.Blazor.Utils;

/// <summary>
/// Represents a set of extension methods for the IPiletService.
/// </summary>
public static class PiletServiceExtensions
{
    /// <summary>
    /// Gets the current access token of the user.
    /// Important: This only works if the "getAccessToken" API is available on the pilet API.
    /// To make this available you'd need to have a plugin such as piral-oidc or piral-oauth2 installed
    /// in your shell.
    /// </summary>
    public static Task<string> GetAccessToken(this IPiletService service)
    {
        return service.Call<string>("getAccessToken");
    }
}
