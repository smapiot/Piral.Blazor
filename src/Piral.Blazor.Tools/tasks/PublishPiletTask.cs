using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Piral.Blazor.Tools.Tasks
{
    public class PublishPiletTask : Task
    {
        #region Globals

        private static bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        private static string npx = isWindows ? "cmd.exe" : "npx";
        private static string npxPrefix = isWindows ? "/c npx.cmd " : "";

        #endregion

        #region Parameters

        [Required]
        public string ProjectName { get; set; }

        [Required]
        public string Source { get; set; }

        [Required]
        public string Target { get; set; }

        [Required]
        public string Framework { get; set; }

        [Required]
        public string FrameworkMoniker { get; set; }

        [Required]
        public string FeedUrl { get; set; }

        public string FeedApiKey { get; set; }

        #endregion

        #region Helper Properties

        private string ToolsVersion => typeof(ManagePiletTask).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        private string FolderProfilePath => Path.Combine(Source, "Properties", "PublishProfiles", "FolderProfile.pubxml");

        private string TargetDir => Path.Combine(Source, Target);

        private string ProjectDir => Path.Combine(TargetDir, ProjectName);

        #endregion

        #region Helpers

        private string GetPublishFolder()
        {
            var folderPath = FolderProfilePath;

            if (File.Exists(folderPath))
            {
                using var stream = File.OpenRead(folderPath);
                using var reader = XmlReader.Create(stream);

                reader.MoveToContent();

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "PublishUrl")
                        {
                            return Path.Combine(Source, reader.ReadInnerXml());
                        }
                    }
                }
            }

            return null;
        }

        private void RunPublish()
        {
            var publishFolder = GetPublishFolder();

            if (publishFolder is not null)
            {
                var root = Path.Combine(publishFolder, "wwwroot");
                var target = ProjectDir;
                var outdir = Path.Combine(target, "dist");
                var auth = string.IsNullOrEmpty(FeedApiKey) ? "--interactive" : $"--api-key {FeedApiKey}";

                // Remove existing "dist" directory, if any
                if (Directory.Exists(outdir))
                {
                    Directory.Delete(outdir, true);
                }

                // Build the pilet
                Run(npx, target, $"{npxPrefix}pilet build");

                // Replace with files from publish
                CompareAndReplace(outdir, root);

                // Pack and publish
                Run(npx, target, $"{npxPrefix}pilet pack");
                Run(npx, target, $"{npxPrefix}pilet publish --url {FeedUrl} {auth}");
            }
        }

        private void CompareAndReplace(string outdir, string root)
        {
            var files = Directory.GetFiles(outdir, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relName = file.Replace(outdir, "");
                var newName = root + relName;

                if (File.Exists(newName))
                {
                    File.Copy(newName, file, true);
                }
            }
        }

        private void Run(string cmd, string cwd, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = cmd,
                WorkingDirectory = cwd,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            // Prevents running dotnet build unnecessarily
            startInfo.EnvironmentVariables["PIRAL_BLAZOR_LAST_BUILD"] = "1";

            var proc = new Process
            {
                StartInfo = startInfo,
            };

            proc.OutputDataReceived += (sender, e) => Log.LogCommandLine($"[stdout] {e.Data}");
            proc.ErrorDataReceived += (sender, e) => Log.LogCommandLine($"[stderr] {e.Data}");

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                Log.LogWarning("The provided command '{0} {1}' in '{2}' did not finish successfully. The process might be corrupted.", cmd, arguments, cwd);
            }
        }

        #endregion

        public override bool Execute()
        {
            Log.LogMessage($"Checking the pilet infrastructure (Version={ToolsVersion}, Framework={Framework})...");

            try
            {
                RunPublish();
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
