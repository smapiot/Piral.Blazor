using System;
using System.Threading.Tasks;

namespace Piral.Blazor.Core.Dependencies;

internal class DisposableServiceProvider : IServiceProvider, IDisposable, IAsyncDisposable
{
    private IServiceProvider _inner;
    private Action _onDispose;
    private Func<Task> _onAsyncDispose;

    public DisposableServiceProvider(IServiceProvider inner, Action onDispose = null, Func<Task> onAsyncDispose = null)
    {
        _inner = inner;
        _onDispose = onDispose;
        _onAsyncDispose = onAsyncDispose;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public object GetService(Type serviceType) => _inner.GetService(serviceType);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose of inner provider first.
            if (_inner is IDisposable innerDisposable)
            {
                innerDisposable.Dispose();
            }

            _onDispose?.Invoke();
        }

        _inner = null;
        _onDispose = null;
        _onAsyncDispose = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();

        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_inner is IAsyncDisposable innerDisposableAsync)
        {
            await innerDisposableAsync.DisposeAsync();
        }
        else if (_inner is IDisposable innerDisposable)
        {
            innerDisposable.Dispose();
        }

        if (_onAsyncDispose != null)
        {
            await _onAsyncDispose().ConfigureAwait(false);
        }

        _onAsyncDispose = null;
        _onDispose = null;
        _inner = null;
    }
}
