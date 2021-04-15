using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Piral.Blazor.Analyzer
{
    internal readonly struct Options
    {
        public string BaseDir { get; }
        public string Dir { get; }
        public string DllName { get; }
        public string DllPath { get; }

        public Options(IReadOnlyList<string> args)
        {
            BaseDir = args[0];
            DllName = args[1];

            Dir = Path.GetFullPath(Path.Combine(BaseDir, "bin", "Release", "netstandard2.1"));
            DllPath = Path.Combine(Dir, DllName);
        }
    }

    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var options = new Options(args);

            Loader.LoadFromDirectoryName = options.Dir;
            AppDomain.CurrentDomain.AssemblyResolve += Loader.LoadDependency;
            AppDomain.CurrentDomain.TypeResolve += Loader.LoadDependency;

            var attributeData = Assembly
                .LoadFrom(options.DllPath)
                .GetTypes()
                .SelectMany(t => t.GetCustomAttributesData())
                .ToReadOnly();

            var pages = attributeData.GetAttributeValues("RouteAttribute");
            Console.WriteLine(JsonSerializer.Serialize(new { pages }));
        }
    }

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

        internal static IReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> source)
        {
            return source.ToList().AsReadOnly();
        }
    }

    [Serializable]
    internal class Loader : MarshalByRefObject
    {
        internal static string LoadFromDirectoryName { get; set; }

        internal static Assembly LoadDependency(object sender, ResolveEventArgs args)
        {
            Assembly assembly = null;
            try
            {
                assembly = Assembly.LoadFile(FileNameFromAssemblyName(args.Name));
            }
            catch { }

            return assembly;
        }

        private static string FileNameFromAssemblyName(string p)
        {
            return Path.Combine(LoadFromDirectoryName, p.Split(',')[0] + ".dll");
        }
    }
}
