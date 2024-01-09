using System;

namespace Piral.Blazor.Core;

public interface IQueryParameterValueProvider
{
    void ProvideQueryParameterValues(Type componentType, object instance);
}