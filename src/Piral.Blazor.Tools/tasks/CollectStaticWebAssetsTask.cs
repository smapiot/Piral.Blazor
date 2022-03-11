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
            try
            {
                foreach (string projectName in ProjectsWithStaticFiles)
                {
                    if (AssetPath.Contains(projectName))
                    {
                        var fileName = Path.GetFileName(AssetPath);
                        var folderName = $"{TargetPath}/{projectName}";

                        if (!Directory.Exists(folderName))
                        {
                            Directory.CreateDirectory(folderName);
                        }

                        File.Copy(AssetPath, $"{folderName}/{fileName}", true); 
                        Log.LogMessage($"File '{AssetPath}' copied to '{folderName}/{fileName}'.");  
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
