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
            try
            {
                var piletPackageJsonFile = $"{PiletPath}/package.json";

                if (!File.Exists(piletPackageJsonFile))
                {
                    Log.LogError($"Could not find the pilet's package.json file at '{piletPackageJsonFile}'."); 
                    return false;
                }

                var piralInstanceVersion = GetPiralInstanceVersion();
                var piralInstancePathInPackageJson = $"file:..\\\\{PiralInstancePath.Replace("\\", "\\\\").Replace("/", "\\\\")}";
                var piletPackageJsonText = File.ReadAllText(piletPackageJsonFile).Replace(piralInstancePathInPackageJson, piralInstanceVersion);
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
                Log.LogError($"Could not find Piral instance package.json file at '{piralInstancePackageJsonFile}'."); 
                throw new FileNotFoundException();
            }

            return JsonConvert.DeserializeObject<PackageJsonObject>(File.ReadAllText(piralInstancePackageJsonFile)).Version;
        }
    }
}
