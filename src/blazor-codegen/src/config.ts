import glob from "glob";
import { basename, resolve } from "path";
import { readFile } from "fs";
import { XMLParser } from "fast-xml-parser";
import { configuration, pajson, swajson } from "./constants";
import { ProjectConfig } from "./types";

function getProjectName(Project: any, defaultName: string) {
  if (typeof Project.PropertyGroup === "object" && Project.PropertyGroup) {
    const propertyGroups = Array.isArray(Project.PropertyGroup)
      ? Project.PropertyGroup
      : [Project.PropertyGroup];
    const propertyGroup = propertyGroups.find((p) => p.AssemblyName);

    if (propertyGroup) {
      return propertyGroup.AssemblyName;
    }
  }

  return defaultName;
}

function getPriority(Project: any) {
  if (typeof Project.PropertyGroup === "object" && Project.PropertyGroup) {
    const propertyGroups = Array.isArray(Project.PropertyGroup)
      ? Project.PropertyGroup
      : [Project.PropertyGroup];
    const propertyGroup = propertyGroups.find((p) => p.PiletPriority);

    if (propertyGroup && !isNaN(+propertyGroup.PiletPriority)) {
      return propertyGroup.PiletPriority;
    }
  }

  return "undefined";
}

function getTargetFramework(Project: any, reject: (err: Error) => void) {
  if (typeof Project.PropertyGroup === "object" && Project.PropertyGroup) {
    const propertyGroups = Array.isArray(Project.PropertyGroup)
      ? Project.PropertyGroup
      : [Project.PropertyGroup];
    const propertyGroup = propertyGroups.find((p) => p.TargetFramework);

    if (propertyGroup) {
      return propertyGroup.TargetFramework;
    }
  }

  reject(
    new Error('The project file does not specify a "TargetFramework" property.')
  );
}

function getConfigFolderName(Project: any) {
  if (typeof Project.PropertyGroup === "object" && Project.PropertyGroup) {
    const propertyGroups = Array.isArray(Project.PropertyGroup)
      ? Project.PropertyGroup
      : [Project.PropertyGroup];
    const propertyGroup = propertyGroups.find((p) => p.ConfigFolder);

    if (propertyGroup && !isNaN(+propertyGroup.ConfigFolder)) {
      return propertyGroup.ConfigFolder;
    }
  }

  return "";
}

export function getProjectConfig(projectDir: string) {
  return new Promise<ProjectConfig>((resolvePromise, rejectPromise) => {
    glob(`${projectDir}/*.csproj`, (err, matches) => {
      if (!!err || !matches || matches.length == 0)
        return rejectPromise(
          new Error(`Project file not found. Details: ${err}`)
        );
      if (matches.length > 1)
        return rejectPromise(
          new Error(
            `Only one project file is allowed. You have: ${JSON.stringify(
              matches,
              null,
              2
            )}`
          )
        );
      const path = matches[0];
      const defaultAssetName = basename(matches[0]).replace(".csproj", "");

      readFile(path, "utf8", (err, xmlData) => {
        if (err) {
          rejectPromise(err);
        } else {
          const xmlParser = new XMLParser();
          const { Project } = xmlParser.parse(xmlData);
          const configFolderName = getConfigFolderName(Project);
          const targetFramework = getTargetFramework(Project, rejectPromise);

          resolvePromise({
            projectDir,
            configDir: resolve(projectDir, configFolderName),
            objectsDir: resolve(projectDir, "obj"),
            paFile: resolve(projectDir, "obj", pajson),
            swaFile: resolve(
              projectDir,
              "obj",
              configuration,
              targetFramework,
              swajson
            ),
            targetFramework,
            priority: getPriority(Project),
            projectName: getProjectName(Project, defaultAssetName),
          });
        }
      });
    });
  });
}
