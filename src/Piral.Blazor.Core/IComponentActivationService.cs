using System;
using System.Collections.Generic;

namespace Piral.Blazor.Core
{
    public interface IComponentActivationService
    {
        event EventHandler Changed;

        IEnumerable<ActiveComponent> Components { get; }
    }
}