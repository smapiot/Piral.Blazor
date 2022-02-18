using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Dynamic;
using Piral.Blazor.Tools.Models;

namespace Piral.Blazor.Tools.Tasks
{
    public class SetMonorepoPiralInstanceTask : Task
    {
        [Required]
        public string PiralInstancePath { get; set; }

        [Required]
        public string PiletPath { get; set; }

        public override bool Execute()
        {
            // System.Diagnostics.Debugger.Launch(); 
            try
            {
                var piletPackageJsonFile = $"{PiletPath}/package.json";

                if (!File.Exists(piletPackageJsonFile))
                {
                    Log.LogError($"Could not find pilet package.json file here:'{piletPackageJsonFile}'."); 
                    return false;
                }

                var piralInstanceVersion = GetPiralInstanceVersion();
                var piralInstancePathInPackageJson = $"file:..\\\\{PiralInstancePath.Replace("\\", "\\\\").Replace("/", "\\\\")}";

                var piletPackageJsonText = File.ReadAllText(piletPackageJsonFile); 
                piletPackageJsonText = piletPackageJsonText.Replace(piralInstancePathInPackageJson, piralInstanceVersion);
                File.WriteAllText(piletPackageJsonFile, piletPackageJsonText); 
            }
            catch (Exception error)
            {
                Log.LogError(error.Message);  
                return false;
            }
            return true; 
        }

        private dynamic GetPiralInstanceVersion(){
            var emulatorDirectory = Path.GetDirectoryName(PiralInstancePath);
            var piralInstanceDirectory = emulatorDirectory.Replace("\\dist\\emulator", "");
            var piralInstancePackageJsonFile = $"{piralInstanceDirectory}/package.json";
            
            if (!File.Exists(piralInstancePackageJsonFile)) {
                Log.LogError($"Could not find piral instance package.json file here:'{piralInstancePackageJsonFile}'."); 
                throw new FileNotFoundException();
            }

            var piralInstancePackageJsonData = JsonConvert.DeserializeObject<PackageJsonObject>(File.ReadAllText(piralInstancePackageJsonFile));
            return piralInstancePackageJsonData.Version; 
        }
    }
}
