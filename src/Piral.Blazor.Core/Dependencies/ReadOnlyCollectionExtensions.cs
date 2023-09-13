using System.Collections.Generic;

namespace Piral.Blazor.Core.Dependencies;

internal static class ReadOnlyCollectionExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind)
    {
        for (var i = 0; i < self.Count; i++)
        {
            var element = self[i];

            if (Equals(element, elementToFind))
            {
                return i;
            }
        }

        return -1;
    }
}
