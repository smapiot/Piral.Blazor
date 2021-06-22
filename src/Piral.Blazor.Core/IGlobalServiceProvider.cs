using System;

namespace Piral.Blazor.Core
{
    public interface IGlobalServiceProvider : IServiceProvider
    {
        void AddProvider(IServiceProvider provider);
    }
}
