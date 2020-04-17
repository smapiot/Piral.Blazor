using System;

namespace Piral.Blazor.Core
{
    public class NestedServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _parent;
        private readonly IServiceProvider _current;

        public NestedServiceProvider(IServiceProvider parent, IServiceProvider current)
        {
            _parent = parent;
            _current = current;
        }

        public object GetService(Type serviceType) => 
            _current.GetService(serviceType) ??
            _parent.GetService(serviceType);
    }
}
