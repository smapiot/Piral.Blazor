using System.Collections.Generic;
using System.IO;

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
}
