import glob from "glob";
import { basename, resolve } from "path";
import { readFile } from "fs";
import { XMLParser } from "fast-xml-parser";
import { configuration, pajson, swajson } from "./constants";
import { ProjectConfig } from "./types";

function getProjectName(Project: any, defaultName: string): string {
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

function getPriority(Project: any): string {
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

function getKind(Project: any): string {
  if (typeof Project.PropertyGroup === "object" && Project.PropertyGroup) {
    const propertyGroups = Array.isArray(Project.PropertyGroup)
      ? Project.PropertyGroup
      : [Project.PropertyGroup];
    const propertyGroup = propertyGroups.find((p) => p.PiletKind);

    if (propertyGroup) {
      return propertyGroup.PiletKind;
    }
  }

  return "local";
}

function getTargetFramework(
  Project: any,
  reject: (err: Error) => void
): string {
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

  return "";
}

function getConfigFolderName(Project: any): string {
  if (typeof Project.PropertyGroup === "object" && Project.PropertyGroup) {
    const propertyGroups = Array.isArray(Project.PropertyGroup)
      ? Project.PropertyGroup
      : [Project.PropertyGroup];
    const propertyGroup = propertyGroups.find((p) => p.ConfigFolder);

    if (propertyGroup) {
      return propertyGroup.ConfigFolder;
    }
  }

  return "";
}

function getSharedDependencies(Project: any): Array<string> {
  const sharedDependencies = [];

  if (typeof Project.ItemGroup === "object" && Project.ItemGroup) {
    const itemGroups = Array.isArray(Project.ItemGroup)
      ? Project.ItemGroup
      : [Project.ItemGroup];

    const sharedGroups = itemGroups.filter(group => group['@_Label'] === 'shared');

    for (const group of sharedGroups) {
      if (group.PackageReference) {
        const references = Array.isArray(group.PackageReference)
          ? group.PackageReference
          : [group.PackageReference];

        for (const reference of references) {
          const name = reference['@_Name'];

          if (typeof name === 'string') {
            sharedDependencies.push(name);
          }
        }
      }
    }
  }

  return sharedDependencies;
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
            sharedDependencies: getSharedDependencies(Project),
            targetFramework,
            priority: getPriority(Project),
            kind: getKind(Project),
            projectName: getProjectName(Project, defaultAssetName),
          });
        }
      });
    });
  });
}
