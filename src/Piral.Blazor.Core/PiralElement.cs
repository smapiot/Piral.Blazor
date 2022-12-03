using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Piral.Blazor.Core
{
    public class PiralElement
    {
        public event EventHandler Changed;

        public PiralElement(Type component, IDictionary<string, JsonElement> args)
        {
            Component = component ?? typeof(Empty);
            Args = Component.AdjustArguments(args);
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
        public void UpdateArgs(IDictionary<string, JsonElement> args)
        {
            Args = Component.AdjustArguments(args);
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
}
