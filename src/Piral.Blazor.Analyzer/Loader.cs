using System;
using System.IO;
using System.Reflection;

namespace Piral.Blazor.Analyzer;

[Serializable]
internal class Loader : MarshalByRefObject
{
    internal static string LoadFromDirectoryName { get; set; }

    internal static Assembly LoadDependency(object sender, ResolveEventArgs args)
    {
        try
        {
            return Assembly.LoadFile(FileNameFromAssemblyName(args.Name));
        }
        catch
        {
            return null;
        }
    }

    private static string FileNameFromAssemblyName(string p)
    {
        return Path.Combine(LoadFromDirectoryName, p.Split(',')[0] + ".dll");
    }
}
