using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Piral.Blazor.Tools.Tasks
{
    public class AddProjectJsonOverwritesTask : Task
    {
        [Required]
        public string PackageJsonPath { get; set; }

        [Required]
        public string OverwritesPath { get; set; }

        public override bool Execute()
        {
            //System.Diagnostics.Debugger.Launch(); 
            try
            { 
                if (!File.Exists(PackageJsonPath)) {
                    return false;
                }
                if (!File.Exists(OverwritesPath)) 
                {
                    return true;
                }
                var result = new JObject();

                var packageJson = JObject.Parse(File.ReadAllText(PackageJsonPath)); 
                var overwritesJson = JObject.Parse(File.ReadAllText(OverwritesPath)); 

                result.Merge(packageJson); 
                result.Merge(overwritesJson); 

                File.WriteAllText(PackageJsonPath, JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            catch (Exception)
            {
                return false;
            }
            return true; 
        }
    }
}
