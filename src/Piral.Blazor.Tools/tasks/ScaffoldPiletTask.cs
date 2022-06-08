using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Piral.Blazor.Tools.Models;
using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Piral.Blazor.Tools.Tasks
{
    public class ScaffoldPiletTask : Task
    {
        [Required]
        public string PiralInstance { get; set; }

        [Required]
        public string ProjectName { get; set; }

        [Required]
        public string CliVersion { get; set; }

        [Required]
        public string ContentFolder { get; set; }

        [Required]
        public string Source { get; set; }

        [Required]
        public string Target { get; set; }

        [Required]
        public string NpmRegistry { get; set; }

        [Required]
        public string Bundler { get; set; }

        [Required]
        public string ToolsVersion { get; set; }

        [Required]
        public string Framework { get; set; }

        [Required]
        public string FrameworkMoniker { get; set; }

        private string PiralInstanceFile => Path.Combine(Source, PiralInstance.Replace('\\', '/'));

        private static string GetRelativePath(string relativeTo, string path)
        {
            var source = new Uri($"{relativeTo}{Path.DirectorySeparatorChar}");
            var target = source.MakeRelativeUri(new Uri(path));
            var rel = Uri
                .UnescapeDataString(target.ToString())
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
            {
                rel = $".{ Path.DirectorySeparatorChar }{ rel }";
            }

            return rel;
        }

        private int GetNpmVersion(string command, string prefix)
        {
            var version = "0.0.0";
            var regex = new Regex(@"\d+\.\d+\.\d+");
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = $"{prefix}--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            while (!proc.StandardOutput.EndOfStream)
            {
                var line = proc.StandardOutput.ReadLine();
                var match = regex.Match(line);

                if (match.Success)
                {
                    version = match.Value;
                }
            }

            proc.WaitForExit();

            var majorVersion = version.Split('.').First();

            if (int.TryParse(majorVersion, out var ver))
            {
                return ver;
            }

            Log.LogError("Could not determine the version of npm. Potentially npm is not installed or too old.");
            return 0;
        }

        public override bool Execute()
        {
            Log.LogMessage($"Checking the pilet infrastructure (Version={ToolsVersion}, Framework={Framework})...");

            if (!Directory.Exists(Target))
            {
                Directory.CreateDirectory(Target);
            }

            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var cmd = isWindows ? "cmd.exe" : "npx";
            var prefix = isWindows ? "/c npx.cmd " : "";
            var npmVersion = GetNpmVersion(cmd, prefix);

            if (npmVersion < 6)
            {
                Log.LogError("At least npm version 6 is required to use Piral.Blazor.");
                return false;
            }
            else if (npmVersion > 8)
            {
                Log.LogWarning("This version of npm has not been tested yet.");
            }

            try
            {
                var target = Path.Combine(Target, ProjectName);
                var infoFile = Path.Combine(target, ".blazorrc");
                // local file paths have to start with .., such as "./foo.tgz" or "../app-shell/foo.tgz"
                var emulator = PiralInstance.StartsWith(".") ? PiralInstanceFile : PiralInstance;

                if (File.Exists(infoFile))
                {
                    var infos = File.ReadAllLines(infoFile);
                    var date = infos
                        .Where(m => m.StartsWith("Date="))
                        .Select(m => Convert.ToDateTime(m.Substring(5))).FirstOrDefault();
                    var version = infos
                        .Where(m => m.StartsWith("Version="))
                        .Select(m => m.Substring(8)).FirstOrDefault();

                    if (version == ToolsVersion && date.CompareTo(DateTime.Now.AddDays(-2)) >= 0)
                    {
                        Log.LogMessage($"Scaffolded infrastructure seems up to date.");
                        return true;
                    }

                    Log.LogMessage($"Updating the pilet infrastructure using piral-cli@{CliVersion}...");

                    Process
                        .Start(cmd, $"{prefix}--package=piral-cli@{CliVersion} -y -- pilet upgrade {emulator} --base {target}")
                        .WaitForExit();
                }
                else
                {
                    Log.LogMessage($"Scaffolding the pilet infrastructure using piral-cli@{CliVersion}...");

                    if (Directory.Exists(target))
                    {
                        Directory.Delete(target, true);
                    }

                    Directory.CreateDirectory(target);

                    Process
                        .Start(cmd, $"{prefix}--package=piral-cli@{CliVersion} -y -- pilet new {emulator} --base {target} --registry {NpmRegistry} --bundler {Bundler} --no-install")
                        .WaitForExit();
                }

                Log.LogMessage($"Updating source files from '{ContentFolder}/**/*'...");
                var files = Directory.GetFiles(ContentFolder, "*", SearchOption.AllDirectories);
                var packageJsonFile = Path.Combine(target, "package.json");
                var packageJsonContent = File.ReadAllText(packageJsonFile);
                var piralInstanceName = JsonConvert.DeserializeObject<PackageJsonObject>(packageJsonContent).Piral.Name;

                foreach (var sourceFile in files)
                {
                    var fn = GetRelativePath(ContentFolder, sourceFile);
                    var targetFile = Path.Combine(target, fn);
                    var content = File.ReadAllText(sourceFile)
                        .Replace("**PiralInstance**", piralInstanceName)
                        .Replace("**BlazorProjectName**", ProjectName)
                        .Replace("**MSBUILD_TargetFramework**", Framework)
                        .Replace("**MSBUILD_TargetFrameworkMoniker**", FrameworkMoniker);

                    File.WriteAllText(targetFile, content, Encoding.UTF8);
                }

                File.WriteAllText(infoFile, $"Date={DateTime.Now}\nVersion={ToolsVersion}");
            }
            catch (Exception error)
            {
                Log.LogError(error.Message);
                return false;
            }

            return true;
        }
    }
}
