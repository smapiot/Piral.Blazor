using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Piral.Blazor.Tools.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Piral.Blazor.Tools.Tasks
{
    public class ManagePiletTask : Task
    {
        #region Globals

        private static bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        private static string npx = isWindows ? "cmd.exe" : "npx";
        private static string npxPrefix = isWindows ? "/c npx.cmd " : "";
        private static string npm = isWindows ? "cmd.exe" : "npm";
        private static string npmPrefix = isWindows ? "/c npm.cmd " : "";
        private static string dotnet = isWindows ? "cmd.exe" : "dotnet";
        private static string dotnetPrefix = isWindows ? "/c dotnet.exe " : "";

        #endregion

        #region Parameters

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

        [Required]
        public string Version { get; set; }

        public string Monorepo { get; set; }

        public string ConfigFolderName { get; set; } = "";

        #endregion

        #region Helper Properties

        private string PiralInstanceFile => Path.Combine(Source, PiralInstance).Replace('\\', '/');

        private string RelativePiralInstanceFile => PiralInstance.Replace('\\', '/');

        private string TargetDir => Path.Combine(Source, Target);

        private string ProjectDir => Path.Combine(TargetDir, ProjectName);

        private string ConfigDir => Path.Combine(Source, ConfigFolderName);

        private bool IsPiralInstanceFile => PiralInstance.StartsWith(".");

        private bool IsMonorepo => Monorepo == "enable";

        private string Emulator => IsPiralInstanceFile ? PiralInstanceFile : PiralInstance;

        #endregion

        #region Utils

        private static string GetRelativePath(string relativeTo, string path)
        {
            var source = new Uri($"{relativeTo}{Path.DirectorySeparatorChar}");
            var target = source.MakeRelativeUri(new Uri(path));
            var rel = Uri
                .UnescapeDataString(target.ToString())
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
            {
                rel = $".{Path.DirectorySeparatorChar}{rel}";
            }

            return rel;
        }

        private string GetPiralInstanceVersion()
        {
            var piralInstanceDirectory = Path.GetDirectoryName(PiralInstance)
                .Replace($"{Path.DirectorySeparatorChar}dist{Path.DirectorySeparatorChar}emulator", "");
            var piralInstancePackageJsonFile = Path.Combine(piralInstanceDirectory, "package.json");

            if (!File.Exists(piralInstancePackageJsonFile))
            {
                Log.LogError($"Could not find Piral instance package.json file at '{piralInstancePackageJsonFile}'.");
                throw new FileNotFoundException();
            }

            var content = File.ReadAllText(piralInstancePackageJsonFile);
            return JsonConvert.DeserializeObject<PackageJsonObject>(content).Version;
        }

        private int GetNpmVersion()
        {
            var version = "0.0.0";
            var regex = new Regex(@"\d+\.\d+\.\d+");

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = npm,
                    Arguments = $"{npmPrefix}--version",
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

        private void CopyConfigurationFiles()
        {
            var configurationFiles = new[] { ".npmrc" };
            var source = ConfigDir;
            var target = ProjectDir;

            Log.LogMessage($"Copying config files from '{source}'...");

            foreach (var configurationFile in configurationFiles)
            {
                var sourceFile = Path.Combine(source, configurationFile);

                if (File.Exists(sourceFile))
                {
                    var targetFile = Path.Combine(target, configurationFile);
                    Log.LogMessage($"Copying file '{configurationFile}' from '{sourceFile}'.");
                    File.Copy(sourceFile, targetFile, true);
                }
            }
        }

        private void CopyContentFiles()
        {
            Log.LogMessage($"Updating source files from '{ContentFolder}/**/*'...");

            var target = ProjectDir;
            var infoFile = Path.Combine(target, ".blazorrc");
            var packageJsonFile = Path.Combine(target, "package.json");
            var files = Directory.GetFiles(ContentFolder, "*", SearchOption.AllDirectories);
            var packageContent = File.ReadAllText(packageJsonFile);
            var packageJson = JsonConvert.DeserializeObject<PackageJsonObject>(packageContent);
            var piralInstanceName = packageJson.Piral.Name;
            var timestamp = JsonConvert.SerializeObject(DateTime.Now).Replace("\"", "");

            foreach (var sourceFile in files)
            {
                var fn = GetRelativePath(ContentFolder, sourceFile);
                var targetFile = Path.Combine(target, fn);
                var content = File.ReadAllText(sourceFile)
                    .Replace("**MSBUILD_PiralInstance**", piralInstanceName)
                    .Replace("**MSBUILD_ProjectFolder**", Source.Replace('\\', '/'))
                    .Replace("**MSBUILD_TargetFramework**", Framework)
                    .Replace("**MSBUILD_TargetFrameworkMoniker**", FrameworkMoniker)
                    .Replace("**MSBUILD_ConfigFolder**", ConfigFolderName);

                File.WriteAllText(targetFile, content, Encoding.UTF8);
            }

            File.WriteAllText(infoFile, $"Date={timestamp}\nVersion={ToolsVersion}\nPiralInstance={Emulator}\nSource={Source}");
        }

        private void EnableAnalyzer()
        {
            var configDir = Path.Combine(Target, ".config");
            var dotnetToolsJson = Path.Combine(configDir, "dotnet-tools.json");

            if (File.Exists(dotnetToolsJson))
            {
                Run(dotnet, ProjectDir, $"{dotnetPrefix}tool update Piral.Blazor.Analyzer --version {ToolsVersion} --local");
            }
            else
            {
                Run(dotnet, Target, $"{dotnetPrefix}new tool-manifest --output .");
                Run(dotnet, ProjectDir, $"{dotnetPrefix}tool install Piral.Blazor.Analyzer --version {ToolsVersion} --local");
            }
        }

        private void CleanMonorepo()
        {
            var target = ProjectDir;
            var packageJsonFile = Path.Combine(target, "package.json");
            var packageLockJsonFile = Path.Combine(target, "package-lock.json");
            var nodeModules = Path.Combine(target, "node_modules");

            if (File.Exists(packageLockJsonFile))
            {
                File.Delete(packageLockJsonFile);
            }

            if (Directory.Exists(nodeModules))
            {
                Directory.Delete(nodeModules, true);
            }

            if (IsPiralInstanceFile)
            {
                Log.LogMessage("Symlinking to the piral instance inside the monorepo...");

                var piralInstanceVersion = GetPiralInstanceVersion();
                var escapedPath = RelativePiralInstanceFile;
                var piralInstancePathInPackageJson = $"file:../{escapedPath}";
                var piletPackageJsonText = File.ReadAllText(packageJsonFile)
                    .Replace(piralInstancePathInPackageJson, piralInstanceVersion);

                File.WriteAllText(packageJsonFile, piletPackageJsonText);
            }
        }

        private void Run(string cmd, string cwd, string arguments)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    WorkingDirectory = cwd,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();
        }

        private bool PreparePilet()
        {
            var target = ProjectDir;
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

                var piralInstance = infos
                    .Where(m => m.StartsWith("PiralInstance="))
                    .Select(m => m.Substring(14)).FirstOrDefault();

                if (version == ToolsVersion && date.CompareTo(DateTime.Now.AddDays(-2)) >= 0 && piralInstance == Emulator)
                {
                    Log.LogMessage($"Scaffolded infrastructure seems up to date.");
                    return false;
                }
                else
                {
                    Log.LogMessage($"Updating the pilet infrastructure using piral-cli@{CliVersion}...");
                    var tag = IsPiralInstanceFile ? PiralInstanceFile : String.Empty;
                    Run(npx, target, $"{npxPrefix}--package=piral-cli@{CliVersion} -y -- pilet upgrade {tag}");
                }
            }
            else
            {
                Log.LogMessage($"Scaffolding the pilet infrastructure using piral-cli@{CliVersion}...");

                if (Directory.Exists(target))
                {
                    Directory.Delete(target, true);
                }

                Directory.CreateDirectory(target);
                Run(npx, target, $"{npxPrefix}--package=piral-cli@{CliVersion} -y -- pilet new {Emulator} --registry {NpmRegistry} --bundler {Bundler} --no-install");
            }

            return true;
        }

        private void CheckNpmVersion()
        {
            var npmVersion = GetNpmVersion();

            if (npmVersion < 6)
            {
                throw new Exception("At least npm version 6 is required to use Piral.Blazor.");
            }
            else if (npmVersion > 8)
            {
                Log.LogWarning("This version of npm has not been tested yet.");
            }
        }

        private void UpdatePackageVersion()
        {
            var target = ProjectDir;
            var packageJsonFile = Path.Combine(target, "package.json");

            if (!File.Exists(packageJsonFile))
            {
                throw new Exception($"The file '{packageJsonFile}' does not exist.");
            }

            if (string.IsNullOrEmpty(Version))
            {
                Log.LogMessage("Keeping the current version set in the 'package.json'.");
            }
            else
            {
                Log.LogMessage("Set version in package.json file...");
                Run(npm, target, $"{npmPrefix}version {Version} --allow-same-version --no-git-tag-version");
            }
        }

        private void InstallDependencies()
        {
            Log.LogMessage("Installing dependencies...");
            Run(npm, ProjectDir, $"{npmPrefix}install --silent");
        }

        #endregion

        public override bool Execute()
        {
            Log.LogMessage($"Checking the pilet infrastructure (Version={ToolsVersion}, Framework={Framework})...");

            var canScaffold = System.Environment.GetEnvironmentVariable("PIRAL_BLAZOR_RUNNING") != "yes";

            try
            {
                CheckNpmVersion();

                if (canScaffold)
                {
                    var requireInstall = PreparePilet();

                    CopyConfigurationFiles();

                    if (requireInstall)
                    {
                        CopyContentFiles();
                    }
                }

                UpdatePackageVersion();

                if (canScaffold)
                {
                    if (IsMonorepo)
                    {
                        CleanMonorepo();
                    }
                    else if (requireInstall)
                    {
                        InstallDependencies();
                    }

                    EnableAnalyzer();
                }

                return true;
            }
            catch (Exception e)
            {
                Log.LogError(e.Message);
                return false;
            }
        }
    }
}
