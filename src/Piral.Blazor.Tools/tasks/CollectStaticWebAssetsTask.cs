using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        [Required]
        public string JsImportsPath { get; set; }

        public override bool Execute()
        {
            Log.LogMessage($"Copying static web assets to blazor project via task...");

            try
            {
                foreach (var projectName in ProjectsWithStaticFiles)
                {
                    if (AssetPath.Contains(projectName))
                    {
                        var fileName = Path.GetFileName(AssetPath);
                        var folderName = $"{TargetPath}/_content/{projectName}/"; 
                        var sourcePath = AssetPath.Replace(fileName, "");

                        if (!Directory.Exists(folderName))
                        {
                            Directory.CreateDirectory(folderName);
                        }

                        // copy all directories
                        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(dirPath.Replace(sourcePath, folderName));
                        }

                        // copy all files
                        foreach (var filePath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
                        {
                            File.Copy(filePath, filePath.Replace(sourcePath, folderName), true);
                        }

                        if (File.Exists(JsImportsPath))
                        {
                            var content = File.ReadAllText(JsImportsPath);
                            var jsImports = JsonConvert.DeserializeObject<List<string>>(content); 
                            var indexTsxFilePath = $"{TargetPath}/index.tsx";
                            var jsImportsString = "";

                            foreach (var jsImport in jsImports)
                            {
                                var importStr = $"import '{jsImport}';";

                                if (!File.ReadAllText(indexTsxFilePath).Contains(importStr))
                                {
                                    jsImportsString += $"{importStr}\n";
                                }
                            }

                            File.WriteAllText(indexTsxFilePath, jsImportsString + File.ReadAllText(indexTsxFilePath));
                        }
                        else
                        {
                            Log.LogError($"The file '{JsImportsPath}' does not exist."); 
                        }

                        Log.LogMessage($"'{AssetPath}' copied to '{folderName}'.");  
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
