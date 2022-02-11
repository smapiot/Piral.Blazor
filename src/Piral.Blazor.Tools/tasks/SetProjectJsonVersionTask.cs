using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
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
            //System.Diagnostics.Debugger.Launch(); 
            try
            {
                if (!File.Exists(PackageJsonPath))
                {
                    return false;
                }
                var packageJsonText = File.ReadAllText(PackageJsonPath); 
                packageJsonText = packageJsonText.Replace(@"""version"": ""1.0.0""", $@"""version"": ""{Version}""");
                File.WriteAllText(PackageJsonPath, packageJsonText); 
            }
            catch (Exception)
            {
                return false;
            }
            return true; 
        }
    }
}
