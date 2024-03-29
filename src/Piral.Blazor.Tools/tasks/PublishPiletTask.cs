﻿using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Piral.Blazor.Tools
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

                // Remove existing npm packages, if any
                foreach (var tgz in Directory.GetFiles(target, "*.tgz", SearchOption.TopDirectoryOnly))
                {
                    File.Delete(tgz);
                }

                // Remove existing "dist" directory, if any
                if (Directory.Exists(outdir))
                {
                    Directory.Delete(outdir, true);
                }

                // Publish with a fresh build to prevent weird behavior as in
                // https://github.com/smapiot/Piral.Blazor/issues/128
                RunPiralCli(target, $"pilet publish --fresh --url {FeedUrl} {auth}");
            }
        }

        private void RunPiralCli(string cwd, string arguments)
        {
            var cmd = npx;
            var args = $"{npxPrefix}{arguments}";
            var startInfo = new ProcessStartInfo
            {
                FileName = cmd,
                WorkingDirectory = cwd,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            var proc = new Process
            {
                StartInfo = startInfo,
            };

            proc.OutputDataReceived += (sender, e) => Log.LogCommandLine($"[piral-cli] {e.Data}");
            proc.ErrorDataReceived += (sender, e) => Log.LogCommandLine($"[piral-cli] {e.Data}");

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                Log.LogWarning("The provided command '{0} {1}' in '{2}' did not finish successfully. The process might be corrupted.", cmd, args, cwd);
            }
        }

        #endregion

        public override bool Execute()
        {
            Log.LogMessage($"Checking the pilet infrastructure (Version={ToolsVersion}, Framework={Framework})...");

            var isRunning = Environment.GetEnvironmentVariable("PIRAL_BLAZOR_RUNNING") == "yes";

            try
            {
                if (!isRunning)
                {
                    RunPublish();
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
