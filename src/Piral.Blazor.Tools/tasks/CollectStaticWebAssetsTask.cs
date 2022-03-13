using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace Piral.Blazor.Tools.Tasks
{
    public class CollectStaticWebAssetsTask : Task
    {
        [Required]
        public string AssetPath { get; set; }

        [Required]
        public string TargetPath { get; set; }

        [Required]
        public string[] ProjectsWithStaticFiles { get; set; }

        public override bool Execute()
        {
            Log.LogMessage($"Copying static web assets to blazor project via task...");

            try
            {
                foreach (string projectName in ProjectsWithStaticFiles)
                {
                    if (AssetPath.Contains(projectName))
                    {
                        var fileName = Path.GetFileName(AssetPath);
                        var folderName = Path.Combine(TargetPath, projectName);
                        var filePath = Path.Combine(folderName, fileName);

                        if (!Directory.Exists(folderName))
                        {
                            Directory.CreateDirectory(folderName);
                        }

                        File.Copy(AssetPath, filePath, true); 
                        Log.LogMessage($"File '{AssetPath}' copied to '{filePath}'.");  
                    }
                }
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
