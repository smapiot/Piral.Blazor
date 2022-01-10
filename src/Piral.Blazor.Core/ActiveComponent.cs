using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Piral.Blazor.Core
{
    public readonly struct ActiveComponent
    {
        private static readonly IDictionary<string, JsonElement> emptyArgs = new Dictionary<string, JsonElement>();

        public ActiveComponent(string componentName, string referenceId, Type component, IDictionary<string, JsonElement> args)
        {
            ComponentName = componentName ?? string.Empty;
            ReferenceId = referenceId ?? string.Empty;
            Component = component ?? typeof(Empty);
            Args = Component.AdjustArguments(args ?? emptyArgs);
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public string ComponentName { get; }

        /// <summary>
        /// Gets the refererence ID to uniquely identifies the instance.
        /// </summary>
        public string ReferenceId { get; }

        /// <summary>
        /// Gets the component's type.
        /// </summary>
        public Type Component { get; }

        /// <summary>
        /// Gets the arguments used for initializing the component.
        /// </summary>
        public IDictionary<string, object> Args { get; }
    }
}
