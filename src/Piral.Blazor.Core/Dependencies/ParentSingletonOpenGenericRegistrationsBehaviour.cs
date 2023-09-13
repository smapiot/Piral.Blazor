namespace Piral.Blazor.Core.Dependencies;

enum ParentSingletonOpenGenericRegistrationsBehaviour
{
    /// <summary>
    /// If there are singleton open generic registerations in the parent container then an exception will be thrown when creating the child container if the underlying container does not support these types of registrations.
    /// For example, the microsoft ServiceProvider based child container does not currently support these because there is currnetly no way to have the child container resolve to the same singleton instance of those as the parent container.
    /// From the exception you can see a list of these unsupported registrations and then work out how to handle them.
    /// </summary>
    ThrowIfNotSupportedByContainer = 0,
    /// <summary>
    /// If there are singleton open generic registerations in the parent container, they will also be registered again in the child container as seperate singletons. This means resolving an open generic type with the same type parameters in the parent and child container will yield two seperate instances of that service.
    /// </summary>
    DuplicateSingletons = 1,
    /// <summary>
    /// If there are singleton open generic registerations in the parent container, they will be omitted from the child container. In other words you won't be able to resolve these services from the built child container.
    /// </summary>
    Omit = 2,
    /// <summary>
    /// If there are singleton open generic registerations in the parent container, requests to the child container for concrete instances of those service types will be delegated to the parent container. This involves a runtime dictionary lookup when services are resolved so may introduce a small performance decrement.
    /// </summary>
    Delegate = 3
}
