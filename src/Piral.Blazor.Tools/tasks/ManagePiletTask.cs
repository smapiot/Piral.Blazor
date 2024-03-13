using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Piral.Blazor.Tools.Models;
using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Piral.Blazor.Tools
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

        public string NpmRegistry { get; set; }

        [Required]
        public string Bundler { get; set; }

        [Required]
        public string Framework { get; set; }

        [Required]
        public string FrameworkMoniker { get; set; }

        [Required]
        public string Version { get; set; }

        public string Monorepo { get; set; }

        public string BundlerVersion { get; set; }

        public string ConfigFolderName { get; set; } = "";

        public string MocksFolderName { get; set; } = "mocks";

        #endregion

        #region Helper Properties

        private string PiralInstanceFile => Path.Combine(Source, PiralInstance).Replace('\\', '/');

        private string RelativePiralInstanceFile => PiralInstance.Replace('\\', '/');

        private string TargetDir => Path.Combine(Source, Target);

        private string ProjectDir => Path.Combine(TargetDir, ProjectName);

        private string ConfigDir => Path.Combine(Source, ConfigFolderName);

        private string MocksDir => Path.Combine(Source, MocksFolderName);

        private bool IsPiralInstanceFile => PiralInstance.StartsWith(".");

        private bool IsMonorepo => Monorepo == "enable";

        private string Emulator => IsPiralInstanceFile ? PiralInstanceFile : PiralInstance;

        private string ToolsVersion => NormalizeVersion(typeof(ManagePiletTask).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

        #endregion

        #region Utils

        private static string NormalizeVersion(string infoVersion)
        {
            var idx = infoVersion.IndexOf('+');

            if (idx != -1)
            {
                return infoVersion.Substring(0, idx);
            }

            return infoVersion;
        }

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
                    CreateNoWindow = true,
                },
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

            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException("npm did not run successful. You'll need to install npm for using Piral.Blazor.");
            }

            var majorVersion = version.Split('.').First();

            if (int.TryParse(majorVersion, out var ver))
            {
                return ver;
            }

            Log.LogError("Could not determine the version of npm. Potentially npm is not installed or too old.");
            return 0;
        }

        private void InitializeNpmConfiguration()
        {
            var configFileName = ".npmrc";
            var source = ConfigDir;
            var target = ProjectDir;
            var registry = NpmRegistry;

            var existingConfigFile = Path.Combine(source, configFileName);

            if (File.Exists(existingConfigFile))
            {
                Log.LogMessage($"Taking '{configFileName}' from '{existingConfigFile}'.");
                var targetFile = Path.Combine(target, configFileName);
                File.Copy(existingConfigFile, targetFile, true);
            }
            else if (!String.IsNullOrEmpty(registry))
            {
                Log.LogMessage($"Creating '{configFileName}' with entry from NpmRegistry.");
                var nl = Environment.NewLine;
                File.AppendAllText(Path.Combine(target, configFileName), $"registry={registry}{nl}always-auth=true", Encoding.UTF8);
            }
        }

        private void CopyContentFiles()
        {
            Log.LogMessage($"Updating source files from '{ContentFolder}/**/*'...");

            var target = ProjectDir;
            var infoFile = Path.Combine(target, ".blazorrc");
            var krasFile = Path.Combine(target, ".krasrc");
            var packageJsonFile = Path.Combine(target, "package.json");
            var piletJsonFile = Path.Combine(target, "pilet.json");
            var files = Directory.GetFiles(ContentFolder, "*", SearchOption.AllDirectories);
            var packageJsonContent = File.ReadAllText(packageJsonFile);
            var timestamp = JsonConvert.SerializeObject(DateTime.Now).Replace("\"", "");
            var packageJson = JsonConvert.DeserializeObject<PackageJsonObject>(packageJsonContent);

            string piralInstanceName;

            if (File.Exists(piletJsonFile))
            {
                var piletJsonContent = File.ReadAllText(piletJsonFile);
                var piletJson = JsonConvert.DeserializeObject<PiletJsonObject>(piletJsonContent);

                if (piletJson.PiralInstances?.Any(x => x.Value.Selected) ?? false)
                {
                    piralInstanceName = piletJson.PiralInstances?.FirstOrDefault(x => x.Value.Selected).Key;
                }
                else
                {
                    piralInstanceName = piletJson.PiralInstances?.Keys.First();
                }
            }
            else
            {
                piralInstanceName = packageJson.Piral?.Name;
            }

            if (piralInstanceName is null)
            {
                throw new InvalidOperationException("The provided Piral instance cannot be found. Something went wrong during scaffolding.");
            }

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
            File.WriteAllText(krasFile, "{\n  \"injectors\": {\n    \"pilet\": {\n      \"meta\": \"meta.json\"\n    }\n  }\n}");
        }

        private void EnableAnalyzer()
        {
            var configDir = Path.Combine(Target, ".config");
            var dotnetToolsJson = Path.Combine(configDir, "dotnet-tools.json");

            if (File.Exists(dotnetToolsJson))
            {
                Run("dotnet", dotnet, ProjectDir, $"{dotnetPrefix}tool update Piral.Blazor.Analyzer --version {ToolsVersion} --local");
            }
            else
            {
                Run("dotnet", dotnet, Target, $"{dotnetPrefix}new tool-manifest --output .");
                Run("dotnet", dotnet, ProjectDir, $"{dotnetPrefix}tool install Piral.Blazor.Analyzer --version {ToolsVersion} --local");
            }
        }

        private void ChangeOutputDirectory()
        {
            var target = ProjectDir;
            var packageJsonFile = Path.Combine(target, "package.json");
            ExtendJson(packageJsonFile, $"{{\"main\":\"dist/index.js\"}}");
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

        private void Run(string id, string cmd, string cwd, string arguments)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    WorkingDirectory = cwd,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                },
            };

            proc.OutputDataReceived += (sender, e) => Log.LogCommandLine($"[{id}] {e.Data}");
            proc.ErrorDataReceived += (sender, e) => Log.LogCommandLine($"[{id}] {e.Data}");

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                Log.LogWarning("The provided command '{0} {1}' in '{2}' did not finish successfully. The process might be corrupted.", cmd, arguments, cwd);
            }
        }

        private bool PreparePilet()
        {
            var target = ProjectDir;
            var infoFile = Path.Combine(target, ".blazorrc");
            var bundlerVersion = BundlerVersion ?? (CliVersion.Contains(".") ? string.Join(".", CliVersion.Split('.').Take(2)) : CliVersion);

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

                if (version != ToolsVersion || piralInstance != Emulator)
                {
                    // Something fundamental changed - let's just delete and scaffold again
                }
                else if (date.CompareTo(DateTime.Now.AddDays(-7)) >= 0)
                {
                    Log.LogMessage($"Scaffolded infrastructure seems up to date.");
                    return false;
                }
                else
                {
                    Log.LogMessage($"Updating the pilet infrastructure using piral-cli@{CliVersion}...");
                    Run("npm", npm, target, $"{npmPrefix}install piral-cli@{CliVersion} --save-dev");
                    Run("npm", npm, target, $"{npmPrefix}install piral-cli-{Bundler}@{bundlerVersion} --save-dev");
                    Run("piral-cli", npx, target, $"{npxPrefix}pilet upgrade");
                    return true;
                }
            }

            Log.LogMessage($"Scaffolding the pilet infrastructure using piral-cli@{CliVersion}...");

            if (Directory.Exists(target))
            {
                Directory.Delete(target, true);
            }

            Directory.CreateDirectory(target);

            InitializeNpmConfiguration();

            Run("npm", npm, target, $"{npmPrefix}init -y");
            Run("npm", npm, target, $"{npmPrefix}install piral-cli@{CliVersion} --save-dev");
            Run("npm", npm, target, $"{npmPrefix}install piral-cli-{Bundler}@{bundlerVersion} --save-dev");
            Run("piral-cli", npx, target, $"{npxPrefix}pilet new \"{Emulator}\" --bundler none --no-install");

            ChangeOutputDirectory();

            return true;
        }

        private void CheckNpmVersion()
        {
            var npmVersion = GetNpmVersion();

            if (npmVersion < 6)
            {
                throw new Exception("At least npm version 6 is required to use Piral.Blazor.");
            }
            else if (npmVersion > 10)
            {
                Log.LogWarning("This version of npm has not been tested yet.");
            }
        }

        private void UpdateKrasSources()
        {
            var krasrc = ".krasrc";
            var krasRcPath = Path.Combine(ProjectDir, krasrc);

            if (File.Exists(krasRcPath) && Directory.Exists(MocksDir))
            {
                var mocksDir = GetRelativePath(ProjectDir, MocksDir).Replace("\\", "/");
                ExtendJson(krasRcPath, $"{{\"sources\":[\"{mocksDir}\"]}}");
            }
        }

        private void OverwritePackageJson()
        {
            var packageJsonFile = Path.Combine(ProjectDir, "package.json");
            var overwritePackageJsonFile = Path.Combine(ConfigDir, "package-overwrites.json");
            MergeJsons(packageJsonFile, overwritePackageJsonFile);
        }

        private void UpdatePackageJson()
        {
            var packageJsonFile = Path.Combine(ProjectDir, "package.json");

            if (!File.Exists(packageJsonFile))
            {
                throw new Exception($"The file '{packageJsonFile}' does not exist.");
            }

            var json = JObject.Parse(File.ReadAllText(packageJsonFile));

            if (!string.IsNullOrEmpty(Version))
            {
                json["version"] = Version;
            }

            var name = json.Property("name")?.Value.Value<string>();

            if (name is not null)
            {
                json["name"] = NormalizeName(name);
            }

            File.WriteAllText(packageJsonFile, JsonConvert.SerializeObject(json, Formatting.Indented));
            Log.LogMessage($"Successfully updated '{packageJsonFile}'.");
        }

        private void OverwriteMetaJson()
        {
            var metaJsonFile = Path.Combine(ProjectDir, "meta.json");
            var overwriteMetaJsonFile = Path.Combine(ConfigDir, "meta-overwrites.json");
            MergeJsons(metaJsonFile, overwriteMetaJsonFile);
        }

        private void OverwriteKrasRc()
        {
            var krasJsonFile = Path.Combine(ProjectDir, ".krasrc");
            var overwriteKrasJsonFile = Path.Combine(ConfigDir, "kras-overwrites.json");
            MergeJsons(krasJsonFile, overwriteKrasJsonFile);
        }

        private void InstallDependencies()
        {
            Log.LogMessage("Installing dependencies...");
            Run("npm", npm, ProjectDir, $"{npmPrefix}install --silent");
        }

        private void InitiateFirstBuild()
        {
            Log.LogMessage("Initiating first build...");
            Run("npm", npm, ProjectDir, $"{npmPrefix}run build");
        }

        private void UpdateAuxiliaryFiles()
        {
            UpdatePackageJson();
            UpdateKrasSources();
            OverwritePackageJson();
            OverwriteMetaJson();
            OverwriteKrasRc();
        }

        #endregion

        #region Helpers

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "pilet";
            }

            name = name.ToLowerInvariant();
            name = name.Replace(' ', '-');

            foreach (var chr in "~)('!*".ToCharArray())
            {
                name = name.Replace($"{chr}", "");
            }

            if (name.StartsWith("@"))
            {
                var rest = name.Substring(1);
                var solidus = rest.IndexOf('/');

                if (solidus == -1 || solidus == rest.Length - 1)
                {
                    return NormalizeName(rest);
                }

                var front = ReplaceUrlCharacters(rest.Substring(0, solidus));
                var back = ReplaceUrlCharacters(rest.Substring(solidus + 1));
                name = $"@{front}/{back}";
            }
            else
            {
                name = ReplaceUrlCharacters(name);
            }

            if (name.Length > 214)
            {
                name = name.Substring(0, 214);
            }

            return name;
        }

        private static string ReplaceUrlCharacters(string str)
        {
            return Regex.Replace(HttpUtility.UrlEncode(str), "%[0-9a-f]{2}", "");
        }

        private void MergeJsons(string originalJsonFile, string overwritesJsonFile)
        {
            var source = Path.GetFileName(originalJsonFile);
            var backup = originalJsonFile + ".original";
            var target = Path.GetFileName(overwritesJsonFile);

            if (!File.Exists(originalJsonFile))
            {
                throw new Exception($"The file '{originalJsonFile}' does not exist.");
            }

            if (!File.Exists(overwritesJsonFile))
            {
                Log.LogMessage($"No '{target}' file found to merge into '{source}'.");
                return;
            }

            if (!File.Exists(backup))
            {
                File.Copy(originalJsonFile, backup);
            }

            var result = new JObject();
            var originalJson = JObject.Parse(File.ReadAllText(backup));
            var overwritesJson = JObject.Parse(File.ReadAllText(overwritesJsonFile));

            result.Merge(originalJson);
            result.Merge(overwritesJson);

            File.WriteAllText(originalJsonFile, JsonConvert.SerializeObject(result, Formatting.Indented));
            Log.LogMessage($"Successfully merged '{target}' with '{source}'.");
        }

        private void ExtendJson(string jsonPath, string newJsonContent)
        {
            var source = Path.GetFileName(jsonPath);
            var result = new JObject();
            var addedJson = JObject.Parse(newJsonContent);
            var originalJson = JObject.Parse(File.ReadAllText(jsonPath));

            result.Merge(originalJson);
            result.Merge(addedJson);

            if (!JToken.DeepEquals(result, originalJson))
            {
                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(result, Formatting.Indented));
                Log.LogMessage($"Successfully updated '{source}'.");
            }
        }

        #endregion

        public override bool Execute()
        {
            Log.LogMessage($"Checking the pilet infrastructure (Version={ToolsVersion}, Framework={Framework})...");

            var canScaffold = Environment.GetEnvironmentVariable("PIRAL_BLAZOR_RUNNING") != "yes";

            try
            {
                CheckNpmVersion();

                if (canScaffold)
                {
                    var requireInstall = PreparePilet();

                    if (requireInstall)
                    {
                        CopyContentFiles();
                    }

                    UpdateAuxiliaryFiles();

                    if (IsMonorepo)
                    {
                        CleanMonorepo();
                    }
                    else if (requireInstall)
                    {
                        InstallDependencies();
                    }

                    EnableAnalyzer();

                    if (requireInstall)
                    {
                        InitiateFirstBuild();
                    }
                }
                else
                {
                    UpdateAuxiliaryFiles();
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
