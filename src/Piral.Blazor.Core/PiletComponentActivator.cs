using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Piral.Blazor.Core;

internal class PiletComponentActivator(IModuleContainerService container, ICacheManipulatorService cacheManipulator) : IComponentActivator
{
    private readonly IModuleContainerService _container = container;
    private readonly ICacheManipulatorService _cacheManipulator = cacheManipulator;
    private readonly HashSet<Type> _seen = [];

    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
        }

        if (_seen.Add(componentType))
        {
            var origin = componentType.Assembly;
            var provider = _container.GetProvider(origin);

            // local DI has been found - use it
            if (provider is not null)
            {
                _cacheManipulator.UpdateComponentCache(componentType, provider);
            }
        }

        return (IComponent)Activator.CreateInstance(componentType)!;
    }
}
