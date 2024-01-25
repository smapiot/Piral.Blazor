using Microsoft.Extensions.DependencyInjection;
using System;

namespace Piral.Blazor.Core;

public interface ICacheManipulatorService
{
    void UpdateComponentCache(Type componentType, IServiceProvider provider);
}
