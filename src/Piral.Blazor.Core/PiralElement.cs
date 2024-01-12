using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace Piral.Blazor.Core;

public class PiralElement
{
    public event EventHandler Changed;

    public PiralElement(Type component, NavigationManager navigationManager, IDictionary<string, JsonElement> args)
    {
        Component = component ?? typeof(Empty);
        Args = Component.AdjustArguments(navigationManager, args);
    }

    /// <summary>
    /// Gets the element's type.
    /// </summary>
    public Type Component { get; }

    /// <summary>
    /// Gets or sets the arguments used for initializing the element.
    /// </summary>
    public IDictionary<string, object> Args { get; private set; }

    /// <summary>
    /// Updates the stored arguments and emits a change event.
    /// </summary>
    public void UpdateArgs(NavigationManager navigationManager, IDictionary<string, JsonElement> args)
    {
        Args = Component.AdjustArguments(navigationManager, args);
        HasChanged();
    }

    /// <summary>
    /// Emits the change event, e.g., when the element has been removed.
    /// </summary>
    public void HasChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
