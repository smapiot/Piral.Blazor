using System;
using System.Collections.Generic;
using System.Linq;

namespace Piral.Blazor.Analyzer
{
    internal static class Extensions
    {
        internal static IReadOnlyCollection<string> GetFirstAttributeValue
        (
            this Type type,
            string attributeName
        )
        {
            return type
                .GetCustomAttributesData()
                .Where(ad => ad.AttributeType.Name.Equals(attributeName))
                .Select(ad => ad.ConstructorArguments.FirstOrDefault())
                .Where(ca => ca.Value is string)
                .Select(ca => ca.Value as string)
                .ToReadOnly();
        }

        internal static IDictionary<string, IReadOnlyCollection<string>> MapAttributeValuesFor
        (
            this IEnumerable<Type> types,
            string attributeName
        )
        {
            return types
                .ToDictionary(t => t.FullName, t => t.GetFirstAttributeValue(attributeName))
                .Where(kvp => kvp.Value.Count > 0)
                .ToDictionary(x => x.Key, x => x.Value);
        }


        internal static IReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> source)
        {
            return source.ToList().AsReadOnly();
        }
    }
}
