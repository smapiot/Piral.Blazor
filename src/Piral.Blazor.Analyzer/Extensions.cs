using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Piral.Blazor.Analyzer
{
    internal static class Extensions
    {
        internal static IReadOnlyCollection<string> GetAttributeValues
        (
            this IEnumerable<CustomAttributeData> attributeData,
            string attributeName
        )
        {
            return attributeData
                .Where(ad => ad.AttributeType.Name.Equals(attributeName))
                .SelectMany(ad => ad.ConstructorArguments)
                .Where(ca => ca.Value is string)
                .Select(ca => ca.Value as string)
                .ToReadOnly();
        }

        internal static IReadOnlyCollection<CustomAttributeData> GetAllAttributeData(this Assembly assembly)
        {
            return assembly
                .GetTypes()
                .SelectMany(t => t.GetCustomAttributesData())
                .ToReadOnly();
        }

        internal static IReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> source)
        {
            return source.ToList().AsReadOnly();
        }
    }
}
