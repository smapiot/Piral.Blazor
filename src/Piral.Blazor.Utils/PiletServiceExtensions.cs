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

    /// <summary>
    /// Gets the currently stored data item using the "getData" pilet API.
    /// </summary>
    public static Task<T> GetDataValue<T>(this IPiletService service, string name)
    {
        return service.Call<T>("getData", name);
    }

    /// <summary>
    /// Sets the stored data item using the "setData" pilet API.
    /// </summary>
    public static Task<bool> SetDataValue<T>(this IPiletService service, string name, T value)
    {
        return service.Call<bool>("setData", name, value);
    }

    /// <summary>
    /// Shows the modal registered with the given name.
    /// Important: This only works if the "showModal" API is available on the pilet API.
    /// To make this available you'd need to have the piral-modals plugin installed in your shell.
    /// </summary>
    public static async Task ShowModal<T>(this IPiletService service, string name, T options)
    {
        await service.Call<object>("showModal", name, options);
    }

    /// <summary>
    /// Shows the notification message.
    /// Important: This only works if the "showNotification" API is available on the pilet API.
    /// To make this available you'd need to have the piral-notifications plugin installed in your shell.
    /// </summary>
    public static async Task ShowNotification<T>(this IPiletService service, string content, T options)
    {
        await service.Call<object>("showNotification", content, options);
    }
}
