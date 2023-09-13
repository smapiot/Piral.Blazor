using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Piral.Blazor.Analyzer;

internal static class Program
{
    internal static void Main(string baseDir, string dllName, string targetFramework, string configuration = "Debug")
    {
        var dir = Path.GetFullPath(Path.Combine(baseDir, "bin", configuration, targetFramework));
        var dllPath = Path.Combine(dir, dllName);

        SetupLoader(dir);

        var types = Assembly
            .LoadFrom(dllPath)
            .GetTypes();

        var routes = ExtractRoutes(types);
        var extensions = ExtractExtensions(types);
        Console.WriteLine(JsonSerializer.Serialize(new { routes, extensions }));
    }

    private static IDictionary<string, IReadOnlyCollection<string>> ExtractExtensions(Type[] types)
    {
        return types.MapAttributeValuesFor("PiralExtensionAttribute");
    }

    private static IEnumerable<string> ExtractRoutes(IEnumerable<Type> types)
    {
        return types.SelectMany(t => t.GetFirstAttributeValue("RouteAttribute"));
    }

    private static void SetupLoader(string directory)
    {
        Loader.LoadFromDirectoryName = Path.Combine(directory, "wwwroot", "_framework");
        AppDomain.CurrentDomain.AssemblyResolve += Loader.LoadDependency;
        AppDomain.CurrentDomain.TypeResolve += Loader.LoadDependency;
    }
}
