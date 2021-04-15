using System;
using System.Reflection;
using System.Text.Json;

namespace Piral.Blazor.Analyzer
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var options = new Options(args);
            
            SetupLoader(options.Dir);

            var attributeData = Assembly
                .LoadFrom(options.DllPath)
                .GetAllAttributeData();
            
            var pages = attributeData.GetAttributeValues("RouteAttribute");
            Console.WriteLine(JsonSerializer.Serialize(new { pages }));
        }

        private static void SetupLoader(string directory)
        {
            Loader.LoadFromDirectoryName = directory;
            AppDomain.CurrentDomain.AssemblyResolve += Loader.LoadDependency;
            AppDomain.CurrentDomain.TypeResolve += Loader.LoadDependency;
        }
    }
}
