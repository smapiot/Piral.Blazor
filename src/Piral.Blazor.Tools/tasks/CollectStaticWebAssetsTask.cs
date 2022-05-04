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
                foreach (string projectName in ProjectsWithStaticFiles)
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
                        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(dirPath.Replace(sourcePath, folderName));
                        }

                        // copy all files
                        foreach (string filePath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
                        {
                            File.Copy(filePath, filePath.Replace(sourcePath, folderName), true);
                        }

                        if(File.Exists(JsImportsPath)){
                            var jsImports = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(JsImportsPath)); 
                            var indexTsxFielPath = $"{TargetPath}/index.tsx";
                            var jsImportsString = "";
                            foreach (var jsImport in jsImports)
                            {
                                if(!File.ReadAllText(indexTsxFielPath).Contains($"import '{jsImport}';"))
                                {
                                    jsImportsString += $"import '{jsImport}';\n";
                                }
                            }
                            File.WriteAllText(indexTsxFielPath, jsImportsString + File.ReadAllText(indexTsxFielPath));
                        } else{
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
