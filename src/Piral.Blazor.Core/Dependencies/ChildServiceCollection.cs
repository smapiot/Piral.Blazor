using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Piral.Blazor.Core.Dependencies;

/// <summary>
/// An implementation of <see cref="IServiceCollection"/> that provides a unified view of <see cref="ServiceDescriptor"/> in a parent <see cref="IServiceCollection"/> in addition to those added directly to the child <see cref="ChildServiceCollection"/> itself. You can access all the descriptors as if it was a single collection, however you can also get only the descriptors added to the child collection which is helpful for configuring child containers.
/// </summary>
internal class ChildServiceCollection : IChildServiceCollection
{
    private readonly List<ServiceDescriptor> _descriptors = new();

    public ChildServiceCollection(IReadOnlyList<ServiceDescriptor> parent) => Parent = parent;

    public ChildServiceCollection(IReadOnlyList<ServiceDescriptor> parent, IEnumerable<ServiceDescriptor> childDescriptors) : this(parent) => _descriptors.AddRange(childDescriptors);

    /// <inheritdoc />
    public int Count => _descriptors.Count + Parent.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    public IReadOnlyList<ServiceDescriptor> Parent { get; private set; }

    /// <inheritdoc />
    public ServiceDescriptor this[int index]
    {
        get
        {
            var parentCount = Parent.Count;

            if (index < parentCount)
            {
                return Parent.ElementAt(index);
            }
            else
            {
                var newIndex = index - parentCount;
                return _descriptors[newIndex];
            }
        }
        set
        {
            var parentCount = Parent.Count;

            // can't update indexes that belong to parent.
            if (index < parentCount)
            {
                /// throwing `ArgumentOutOfRangeException` instead of `IndexOutOfRangeException` to make consistent with IList.
                throw new ArgumentOutOfRangeException("The index belongs to the parent collection which is readonly.");
            }
            var newIndex = index - parentCount;

            _descriptors[newIndex] = value;
        }
    }

    /// <summary>
    /// Clears any service descriptors added this collection but does not clear the parent collection.
    /// </summary>
    public void Clear() => _descriptors.Clear();

    /// <summary>
    /// Check whether the descriptor is contained either in the parent or in this child collection.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(ServiceDescriptor item) => Parent.Contains(item) || _descriptors.Contains(item);

    /// <inheritdoc />
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        // copy any from the parent
        var current = 0;
        foreach (var item in Parent)
        {
            array[arrayIndex + current] = item;
            current += 1;
        }

        if (_descriptors.Any())
        {
            var parentCount = Parent.Count;
            _descriptors.CopyTo(array, arrayIndex + parentCount);
        }
    }

    /// <summary>
    /// Removes the service descriptor from this child collection, but will not remove it from the parent collection if it exists there, as that is not modifiable.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Remove(ServiceDescriptor item) => _descriptors.Remove(item);

    /// <inheritdoc />
    public IEnumerator<ServiceDescriptor> GetEnumerator() => Parent.Concat(_descriptors).GetEnumerator();

    /// <summary>
    /// Adds a service descriptor to the child collection.
    /// </summary>
    /// <param name="item"></param>
    void ICollection<ServiceDescriptor>.Add(ServiceDescriptor item) => _descriptors.Add(item);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public int IndexOf(ServiceDescriptor item)
    {
        var index = Parent.IndexOf(item);

        if (index == -1)
        {
            index = _descriptors.IndexOf(item) + Parent.Count; // offset from parent which is readonly.
        }

        return index;
    }

    /// <summary>
    /// Adds a service descript to the child service collection a the speficfied inf
    /// </summary>
    /// <param name="index"></param>
    /// <param name="item"></param>
    public void Insert(int index, ServiceDescriptor item)
    {
        var parentCount = Parent.Count;

        // can't update indexes that belong to parent.
        if (index < parentCount)
        {
            /// throwing `ArgumentOutOfRangeException` instead of `IndexOutOfRangeException` to make consistent with IList.
            throw new ArgumentOutOfRangeException("The index belongs to the parent collection which is readonly.");
        }

        var newIndex = index - parentCount;
        _descriptors.Insert(newIndex, item);
    }

    /// <summary>
    /// Removes the service descriptor from the collection at the specified index. The index must correspond to a service added to the child collection and not a service in the parent collection as the parent collection is not modifiable.
    /// </summary>
    /// <param name="index"></param>
    public void RemoveAt(int index)
    {
        var parentCount = Parent.Count;

        // can't update indexes that belong to parent.
        if (index < parentCount)
        {
            /// throwing `ArgumentOutOfRangeException` instead of `IndexOutOfRangeException` to make consistent with IList.
            throw new ArgumentOutOfRangeException("The index belongs to the parent collection which is readonly.");
        }

        var newIndex = index - parentCount;
        _descriptors.RemoveAt(newIndex);
    }

    public IEnumerable<ServiceDescriptor> ChildDescriptors => _descriptors;

    public IEnumerable<ServiceDescriptor> ParentDescriptors => Parent;

    public IChildServiceCollection ConfigureServices(Action<IServiceCollection> configureServices)
    {
        configureServices?.Invoke(this);
        return this;
    }

    #region Methods that modi parent services return a new collection

    /// <summary>
    /// Calls to <see cref="Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAdd"/> within the <paramref name="configureServices"/> will not be prevented from succeeding if descriptors for the same service exist in parent services matching the predicate.
    /// If any such "duplicate" descriptors are added, they are then removed from the parent level service descriptors (so only will exist at child level) in the returned <see cref="IChildServiceCollection"/>.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="configureServices"></param>
    /// <returns></returns>
    public IChildServiceCollection AutoPromoteChildDuplicates(Func<ServiceDescriptor, bool> predicate, Action<IChildServiceCollection> configureServices, Func<ServiceDescriptor, bool> promotePredicate = null)
    {
        // allows TryAdd() to succeed where it wouldn't have previously.
        var toExclude = Parent.Where(predicate).ToArray();
        var filteredParent = Parent.Except(toExclude).ToImmutableList();
        var concatParent = filteredParent.Concat(ChildDescriptors).ToImmutableList();

        var newlyAddedColl = new ChildServiceCollection(concatParent);
        configureServices(newlyAddedColl);
        var added = newlyAddedColl.ChildDescriptors;

        var newChildDescripors = _descriptors.Concat(added);

        // Remove the services from the parent that have been promoted to the child.
        var promoted = toExclude
            .Join(added, (i) => i.ServiceType, o => o.ServiceType, (a, b) => a)
            .Where(a => promotePredicate == null ? true : promotePredicate(a))
            .ToArray();

        var newParent = Parent.Except(promoted).ToImmutableList();
        return new ChildServiceCollection(newParent, newChildDescripors);
    }

    /// <summary>
    /// Removes parent level <see cref="ServiceDescriptor"/> that match the predicate,
    /// </summary>
    /// <remarks>When building child containers backed by <see cref="ServiceProvider"/></remarks> this is often a necessary step for any parent level "singleton open generic" registrations
    /// because they can't currently be resolved from a child container to an existing parent instance, 
    /// and so have to be registered as seperate instances in the child, or omitted / removed.
    public IChildServiceCollection RemoveParentDescriptors(Func<ServiceDescriptor, bool> parentFilterPredicate)
    {
        var toRemove = Parent.Where(parentFilterPredicate);
        var newParent = Parent.Except(toRemove).ToImmutableList();
        return new ChildServiceCollection(newParent, _descriptors);
    }

    public IChildServiceCollection RemoveParentDescriptors(Type serviceType) => RemoveParentDescriptors(a => a.ServiceType == serviceType);

    #endregion
}
