using System;
using System.Collections.Generic;

namespace Piral.Blazor.Core
{
    public interface IComponentActivationService
    {
        /// <summary>
        /// The handler to monitor when the active components changed.
        /// </summary>
        event EventHandler Changed;

        /// <summary>
        /// The currently active components.
        /// </summary>
        IEnumerable<ActiveComponent> Components { get; }
    }
}