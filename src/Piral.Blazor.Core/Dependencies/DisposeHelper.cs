using System;
using System.Threading.Tasks;

namespace Piral.Blazor.Core.Dependencies;

internal static class DisposeHelper
{
    public static async Task DisposeAsyncIfImplemented(object objectToBeDisposed)
    {
        if (objectToBeDisposed is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            DisposeIfImplemented(objectToBeDisposed);
        }
    }

    public static void DisposeIfImplemented(object objectToBeDisposed)
    {
        if (objectToBeDisposed is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
