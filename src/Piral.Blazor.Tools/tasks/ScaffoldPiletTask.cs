using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using Piral.Blazor.Tools.Models;

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

        private static string GetRelativePath(string relativeTo, string path)
        {
            var source = new Uri(relativeTo);
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

        public override bool Execute()
        {
            Log.LogMessage($"Checking the pilet infrastructure (Version={ToolsVersion}, Framework={Framework})...");

            if (!Directory.Exists(Target))
            {
                Directory.CreateDirectory(Target);
            }

            try
            {
                var target = Path.Combine(Target, ProjectName);
                var infoFile = Path.Combine(target, ".blazorrc");

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
                        .Start("npx", $"--package=piral-cli@{CliVersion} -y -- pilet update latest --base {target}")
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
                        .Start("npx", $"--package=piral-cli@{CliVersion} -y -- pilet new {PiralInstance} --base {target} --registry {NpmRegistry} --bundler {Bundler} --no-install")
                        .WaitForExit();
                }

                Log.LogMessage($"Updating source files from '{ContentFolder}/**/*'...");
                var files = Directory.GetFiles(ContentFolder, "*", SearchOption.AllDirectories);

                foreach (var sourceFile in files)
                {
                    var fn = GetRelativePath(ContentFolder, sourceFile);
                    var targetFile = Path.Combine(target, fn);
                    var content = File.ReadAllText(sourceFile)
                        .Replace("**PiralInstance**", PiralInstance)
                        .Replace("**BlazorProjectName**", ProjectName)
                        .Replace("**MSBUILD_TargetFramework**", Framework)
                        .Replace("**MSBUILD_TargetFrameworkMoniker**", FrameworkMoniker);

                    File.WriteAllText(targetFile, content, Encoding.UTF8);
                }

                File.WriteAllText(infoFile, $"Date={DateTime.Now.ToString()}\nVersion={ToolsVersion}");
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
