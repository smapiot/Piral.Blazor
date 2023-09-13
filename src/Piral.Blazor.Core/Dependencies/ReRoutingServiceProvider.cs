using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Piral.Blazor.Core.Dependencies;

internal class ReRoutingServiceProvider : IServiceProvider
{
    private readonly IServiceProvider _defaultServiceProvider;
    private readonly Dictionary<Type, IServiceProvider> _routes = new();
    private readonly HashSet<Type> _openGenericServiceTypes = new();

    private ImmutableDictionary<Type, IServiceProvider> _openGenericTypeMappingCache = ImmutableDictionary<Type, IServiceProvider>.Empty;
    private bool _hasOpenGenericTypes = false;

    public ReRoutingServiceProvider(IServiceProvider defaultServiceProvider)
    {
        _defaultServiceProvider = defaultServiceProvider ?? throw new ArgumentNullException(nameof(defaultServiceProvider));
    }

    public ReRoutingServiceProvider ReRoute(IServiceProvider newDestinationServiceProvider, params Type[] serviceTypes) => ReRoute(newDestinationServiceProvider, serviceTypes.AsEnumerable());

    public ReRoutingServiceProvider ReRoute(IServiceProvider newDestinationServiceProvider, IEnumerable<Type> serviceTypes)
    {
        foreach (var serviceType in serviceTypes)
        {
            // TODO: what about if the same service type is registered multiple times,
            // in that case, we need to support resolving IEnumerable<TServiceType> as well?
            _routes[serviceType] = newDestinationServiceProvider;

            if (!serviceType.IsClosedType())
            {
                // need to build a reverse mapping for open generic types
                // so that when a closed type is requested we can map it back to the open type to be requested.
                _openGenericServiceTypes.Add(serviceType);
            }

            _hasOpenGenericTypes = _openGenericServiceTypes.Any();
        }

        return this;
    }

    public object GetService(Type serviceType)
    {
        // We need to ascertain if this type is an instance of an open generic service.

        // TODO: what about if the same service type is registered multiple times,
        // in that case, do we need to think about automatically supporting the resolution of IEnumerable<TServiceType> as well?

        // Suppose an open generic is registered like ILogger<>
        // a service may be requested such as ILogger<string>
        // In that case we must detect that ILogger<string> being requested maps to the route for ILogger<>
        // But we don't want to have to do this check all the time so
        //   - A)
        //       i) If there are no open generic registrations to worry about - fast path.
        //       ii) If the service type beign resolved is not generic - fast path.
        //   - B) If it is generic,
        //       i) work out if it relates to an open generic registration.
        //       ii) above case takes a lot of work, so keep a cache for
        //         a) generic types we've already checked and found don't need to be forwarded to an open type registration route - so we can skip this check in future.
        //         b) generic types we've already checked, and found do need to be forwarded to an open generic type registrations - so we can short cut to the previously ascertained answer.
        //          *)  Don't lock for cache updates - prefer a cache miss, cache should slowly grow to prevent cache misses in future concurrent cases.
        //   

        // A
        // i and ii)
        if (!_hasOpenGenericTypes || !serviceType.IsGenericType)
        {
            var sp = LookupServiceProvider(serviceType);
            return sp.GetService(serviceType);
            //return Resolve(serviceType);
        }

        // ii a & b)
        if (_openGenericTypeMappingCache.TryGetValue(serviceType, out var mappedSp))
        {
            return mappedSp.GetService(serviceType);
        }

        // i)
        foreach (var item in _openGenericServiceTypes)
        {
            if (serviceType.IsAssignableToGenericType(item))
            {
                var sp = _routes[item];
                // *)
                _openGenericTypeMappingCache = _openGenericTypeMappingCache.SetItem(serviceType, sp);
                return sp.GetService(serviceType);
            }
        }

        // *)
        mappedSp = LookupServiceProvider(serviceType);
        _openGenericTypeMappingCache = _openGenericTypeMappingCache.Add(serviceType, mappedSp);
        return mappedSp.GetService(serviceType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IServiceProvider LookupServiceProvider(Type serviceType)
    {
        if (_routes.TryGetValue(serviceType, out var sp))
        {
            return sp;
        }
        else
        {
            return _defaultServiceProvider;
        }
    }
}
