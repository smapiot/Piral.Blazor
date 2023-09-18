using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piral.Blazor.Core.Dependencies;

internal static class ServiceCollectionExtensions
{
    public static IServiceProvider CreateChildServiceProvider(this IServiceProvider parentServiceProvider, IServiceCollection parentServices, Action<IChildServiceCollection> configureChildServices)
    {
        var childServices = new ChildServiceCollection(parentServices.ToImmutableList());
        configureChildServices?.Invoke(childServices);
        var childContainer = childServices.BuildChildServiceProvider(parentServiceProvider);
        return childContainer;
    }

    public static IServiceProvider BuildChildServiceProvider(this IChildServiceCollection childServiceCollection, IServiceProvider parentServiceProvider)
    {
        // add all the same registrations that are in the parent to the child,
        // but rewrite them to resolve from the parent IServiceProvider.
        var parentRegistrations = childServiceCollection.ParentDescriptors;
        var reWrittenServiceCollection = new ServiceCollection();
        // obtain a new scope from the parent that can be safely used by the child for the lifetime of the child.
        var parentScope = parentServiceProvider.CreateScope();

        foreach (var item in parentRegistrations)
        {
            var rewrittenDescriptor = CreateChildDescriptorForExternalService(item, parentScope.ServiceProvider);

            if (rewrittenDescriptor != null)
            {
                reWrittenServiceCollection.Add(rewrittenDescriptor);
            }
        }

        // Child service descriptors can be added "as-is"
        foreach (var item in childServiceCollection.ChildDescriptors)
        {
            reWrittenServiceCollection.Add(item);
        }

        var childSp = reWrittenServiceCollection.BuildServiceProvider();
        var innerSp = childSp;

        // Make sure we dispose any parent sp scope that we have leased, when the IServiceProvider is disposed.
        void onDispose()
        {
            // dispose of child sp first, then dispose parent scope that we leased to support the child sp.
            DisposeHelper.DisposeIfImplemented(childSp);
            parentScope.Dispose();
        };

        async Task onDisposeAsync()
        {
            // dispose of child sp first, then dispose parent scope that we leased to support the child sp.
            await DisposeHelper.DisposeAsyncIfImplemented(childSp);
            await DisposeHelper.DisposeAsyncIfImplemented(parentScope);
        }

        var disposableSp = new DisposableServiceProvider(innerSp, onDispose, onDisposeAsync);
        return disposableSp;
    }

    private static ServiceDescriptor CreateChildDescriptorForExternalService(ServiceDescriptor item, IServiceProvider parentServiceProvider)
    {
        // For any services that implement IDisposable, they they will be tracked by Microsofts `ServiceProvider` when it creates them.
        // For a child container, we want the child container to be responsible for the objects lifetime, not the parent container.
        // So we must register the service in the child container. This means the child container will create the instance.
        // This should be ok for "transient" and "scoped" registrations because it shouldn't matter which container creates the instance.
        // However for "singleton" registrations, we want to assume that the singleton should be a "global" singleton - so we don't want the
        // child container to create an instance for that service by default. If the user wants the service to be "singleton at the child container level"
        // then they can add a registration to the ChildServiceCollection for this (which will override anything we do here to amend the parent level registration).
        if (item.Lifetime == ServiceLifetime.Transient || item.Lifetime == ServiceLifetime.Scoped)
        {
            return item;
        }

        if (item.ImplementationInstance != null)
        {
            // global singleton instance already provided with lifetime managed externally so can just re-use this registration.
            return item;
        }

        // We don't have an "open generic type" singleton service definition do we?
        // please say we don't because things will get... awkward.
        if (item.ServiceType.IsClosedType())
        {
            // oh goodie
            // get an instance of the singleton from the parent container - so its owned by the parent.
            // then register this instance as an externally owned instance in this child container.
            // child container won't then try and own it.                
            var singletonInstance = parentServiceProvider.GetRequiredService(item.ServiceType);
            var serviceDescriptor = new ServiceDescriptor(item.ServiceType, singletonInstance); // by providing the instance, the child container won't manage this object instances lifetime (i.e call Dispose if its IDisposable).
            return serviceDescriptor;
        }

        // allow the open generic singleton registration to be added again to this child again resulting in additional singleton instance at child scope.
        return item;
    }
}
