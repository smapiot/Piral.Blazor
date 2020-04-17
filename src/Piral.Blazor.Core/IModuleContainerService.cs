using System;

namespace Piral.Blazor.Core
{
    public interface IModuleContainerService
    {
        void ConfigureComponent(Type type, IServiceProvider provider);
    }
}
