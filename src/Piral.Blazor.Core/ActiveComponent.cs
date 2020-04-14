using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;

namespace Piral.Blazor.Core
{
    public readonly struct ActiveComponent
    {
        private static readonly IDictionary<string, object> emptyArgs = new Dictionary<string, object>();

        public ActiveComponent(string componentName, string referenceId, Type component, IDictionary<string, object> args)
        {
            ComponentName = componentName ?? string.Empty;
            ReferenceId = referenceId ?? string.Empty;
            Component = component ?? typeof(Empty);
            Args = Component.AdjustArguments(args ?? emptyArgs);
        }

        public string ComponentName { get; }

        public string ReferenceId { get; }

        public Type Component { get; }

        public IDictionary<string, object> Args { get; }
    }
}
