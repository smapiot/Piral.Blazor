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
            try
            { 
                if (!File.Exists(PackageJsonPath)) {
                    Log.LogError($"The file '{PackageJsonPath}' does not exist."); 
                    return false;
                }

                if (!File.Exists(OverwritesPath)) 
                {
                    Log.LogMessage("No 'overwrite.package.json' file found to merge into package.json.");
                    return true;
                }

                var result = new JObject();
                var packageJson = JObject.Parse(File.ReadAllText(PackageJsonPath)); 
                var overwritesJson = JObject.Parse(File.ReadAllText(OverwritesPath)); 

                result.Merge(packageJson); 
                result.Merge(overwritesJson); 

                File.WriteAllText(PackageJsonPath, JsonConvert.SerializeObject(result, Formatting.Indented));
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
