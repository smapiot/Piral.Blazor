import glob from "glob";
import { basename, dirname, resolve } from "path";
import { readFile } from "fs/promises";
import { XMLParser } from "fast-xml-parser";

import { configuration, pajson, swajson } from "./constants";
import type { ProjectConfig } from "./types";

function getProjectName(Project: any): string {
  if (typeof Project.PropertyGroup === "object" && Project.PropertyGroup) {
    const propertyGroups = Array.isArray(Project.PropertyGroup)
      ? Project.PropertyGroup
      : [Project.PropertyGroup];
    const propertyGroup = propertyGroups.find((p) => p.AssemblyName);

    if (propertyGroup) {
      return propertyGroup.AssemblyName;
    }
  }

  return undefined;
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

  return undefined;
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

  return undefined;
}

function getTargetFramework(Project: any): string {
  if (typeof Project.PropertyGroup === "object" && Project.PropertyGroup) {
    const propertyGroups = Array.isArray(Project.PropertyGroup)
      ? Project.PropertyGroup
      : [Project.PropertyGroup];
    const propertyGroup = propertyGroups.find((p) => p.TargetFramework);

    if (propertyGroup) {
      return propertyGroup.TargetFramework;
    }
  }

  return undefined;
}

function getImportedProjects(Project: any, basePath: string): Array<string> {
  const projects: Array<string> = [];

  if (typeof Project.Import !== "undefined") {
    const imports = Array.isArray(Project.Import)
      ? Project.Import
      : [Project.Import];

    for (const importItem of imports) {
      const path = importItem["@_Project"];

      if (typeof path === "string") {
        projects.push(resolve(basePath, path));
      }
    }
  }

  return projects;
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

  return undefined;
}

function getSharedDependencies(Project: any): Array<string> {
  const sharedDependencies = [];

  if (typeof Project.ItemGroup === "object" && Project.ItemGroup) {
    const itemGroups = Array.isArray(Project.ItemGroup)
      ? Project.ItemGroup
      : [Project.ItemGroup];

    const sharedGroups = itemGroups.filter(
      (group) => group["@_Label"] === "shared",
    );

    for (const group of sharedGroups) {
      if (group.PackageReference) {
        const references = Array.isArray(group.PackageReference)
          ? group.PackageReference
          : [group.PackageReference];

        for (const reference of references) {
          const name = reference["@_Name"];

          if (typeof name === "string") {
            sharedDependencies.push(name);
          }
        }
      }
    }
  }

  return sharedDependencies;
}

interface ProjectResult {
  targetFramework: string;
  projectDir: string;
  configDir: string;
  sharedDependencies: Array<string>;
  priority: string;
  kind: string;
  projectName: string;
}

async function readProject(path: string) {
  const projectDir = dirname(path);
  const xmlData = await readFile(path, "utf8");
  const xmlParser = new XMLParser({
    ignoreAttributes: false,
    allowBooleanAttributes: true,
  });
  const { Project } = xmlParser.parse(xmlData);
  const importedProject = getImportedProjects(Project, projectDir);
  const result: ProjectResult = {
    projectDir,
    configDir: getConfigFolderName(Project),
    sharedDependencies: getSharedDependencies(Project),
    targetFramework: getTargetFramework(Project),
    priority: getPriority(Project),
    kind: getKind(Project),
    projectName: getProjectName(Project),
  };

  for (const project of importedProject.reverse()) {
    const newResult = await readProject(project);

    Object.entries(newResult).forEach(([name, value]) => {
      if (result[name] === undefined) {
        result[name] = value;
      } else if (Array.isArray(result[name])) {
        result[name].push(...value);
      }
    });
  }

  return result;
}

export function getProjectConfig(projectDir: string) {
  return new Promise<ProjectConfig>((resolvePromise, rejectPromise) => {
    glob(`${projectDir}/*.csproj`, (err, matches) => {
      if (!!err || !matches || matches.length === 0) {
        return rejectPromise(
          new Error(`Project file not found. Details: ${err}`),
        );
      }

      if (matches.length > 1) {
        return rejectPromise(
          new Error(
            `Only one project file is allowed. You have: ${JSON.stringify(
              matches,
              null,
              2,
            )}`,
          ),
        );
      }

      const path = matches[0];
      const defaultAssetName = basename(path).replace(".csproj", "");

      readProject(path)
        .then((result) => {
          if (!result.targetFramework) {
            throw new Error(
              'The project file does not specify a "TargetFramework" property.',
            );
          }

          return {
            projectDir: result.projectDir,
            configDir: resolve(projectDir, result.configDir ?? ""),
            objectsDir: resolve(projectDir, "obj"),
            paFile: resolve(projectDir, "obj", pajson),
            swaFile: resolve(
              projectDir,
              "obj",
              configuration,
              result.targetFramework,
              swajson,
            ),
            sharedDependencies: result.sharedDependencies,
            targetFramework: result.targetFramework,
            priority: result.priority ?? "undefined",
            kind: result.kind ?? "local",
            projectName: result.projectName ?? defaultAssetName,
          };
        })
        .then(resolvePromise, rejectPromise);
    });
  });
}
