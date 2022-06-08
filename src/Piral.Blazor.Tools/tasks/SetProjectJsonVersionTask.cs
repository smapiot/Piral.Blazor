using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace Piral.Blazor.Tools.Tasks
{
    public class SetProjectJsonVersionTask : Task
    {
        [Required]
        public string PackageJsonPath { get; set; }

        [Required]
        public string Version { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrEmpty(Version))
            {
                Log.LogMessage("Keeping the current version set in the 'package.json'.");
                return true;
            }

            Log.LogMessage("Set version in package.json file...");

            try
            {
                if (!File.Exists(PackageJsonPath))
                {
                    Log.LogError($"The file '{PackageJsonPath}' does not exist."); 
                    return false;
                }

                var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
                var command = isWindows ? "cmd.exe" : "npm";
                var prefix = isWindows ? "/c npm.cmd " : "";
                var startInfo = new ProcessStartInfo();

                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        WorkingDirectory = Path.GetDirectoryName(PackageJsonPath),
                        Arguments = $"{prefix}version {Version} --allow-same-version --no-git-tag-version",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                proc.Start();
                proc.WaitForExit();
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
