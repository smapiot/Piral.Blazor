import { resolve, join } from "path";
import { existsSync, readdirSync, readFileSync } from "fs";
import { copyAll } from "./io";
import { findAppDir } from "./piral";
import { checkInstallation } from "./project";
import { diffBlazorBootFiles } from "./utils";
import { checkDotnetVersion, extractDotnetVersion } from "./version";
import { BlazorManifest, StaticAssets } from "./types";
import {
  alwaysIgnored,
  bbjson,
  swajson,
  packageJsonFilename,
  piletJsonFilename,
  variant,
  blazorrc,
} from "./constants";

function toFramework(files: Array<string>) {
  return files.map((n) => `_framework/${n}`);
}

function findInstanceName(piralPiletFolder: string): string {
  const packageJson = require(resolve(piralPiletFolder, packageJsonFilename));
  const piletJsonFilePath = join(piralPiletFolder, piletJsonFilename).replace(
    /\\/g,
    "/"
  );
  const piletJsonFileExists = existsSync(piletJsonFilePath);

  if (piletJsonFileExists) {
    const piletJson = require(resolve(piralPiletFolder, piletJsonFilename));
    const selectedInstance = Object.keys(piletJson.piralInstances).find(
      (key) => piletJson.piralInstances[key].selected
    );

    if (selectedInstance !== undefined) {
      return selectedInstance;
    }

    return Object.keys(piletJson.piralInstances)[0];
  }

  return packageJson.piral.name;
}

function findBlazorVersion(piralPiletFolder: string) {
  const key = "Version=";
  const blazorrcPath = resolve(piralPiletFolder, blazorrc);
  const content = readFileSync(blazorrcPath, "utf8");
  const line = content
    .split("\r")
    .join("")
    .split("\n")
    .find((m) => m.startsWith(key));

  if (typeof line === "string") {
    return line.substring(key.length);
  }

  return undefined;
}

function getBlazorRelease(version: string) {
  const matchVersion = /\d+\.\d+\.\d+/;
  const result = matchVersion.exec(version);

  if (!result) {
    throw new Error(
      "Could not detect version of Blazor. Something does not seem right."
    );
  }

  const [npmBlazorVersion] = result;
  const [blazorRelease] = npmBlazorVersion.split(".");
  return `^${blazorRelease}`;
}

export async function prepare(targetDir: string, staticAssets: StaticAssets) {
  const piralPiletFolder = resolve(__dirname, "..");
  const instanceName = findInstanceName(piralPiletFolder);
  const appdir = findAppDir(piralPiletFolder, instanceName);

  const manifestSource = staticAssets.Assets.find(
    (m) =>
      m.AssetTraitName === "BlazorWebAssemblyResource" &&
      m.AssetTraitValue === "manifest" &&
      m.RelativePath.endsWith(bbjson)
  );

  if (!manifestSource) {
    throw new Error(
      `Could not find the "${bbjson}" in ${swajson}. Something seems to be wrong.`
    );
  }

  // Piral Blazor checks
  const appFrameworkDir = resolve(appdir, "app", "_framework");
  const bbAppShellPath = resolve(appFrameworkDir, bbjson);
  const blazorInAppshell = existsSync(bbAppShellPath);
  const shellPackagePath = resolve(appdir, packageJsonFilename);
  const manifest = manifestSource.Identity;
  const piletManifest: BlazorManifest = require(manifest);
  const bbStandalonePath = `blazor/${variant}/wwwroot/_framework/${bbjson}`;
  const piletDotnetVersion = extractDotnetVersion(piletManifest);
  const standalone = !blazorInAppshell;
  const { satelliteResources } = piletManifest.resources;

  const satellites = Object.keys(satelliteResources || {}).reduce(
    (satellites, name) => {
      const resources = satelliteResources[name];
      const files = Object.keys(resources);
      satellites[name] = toFramework(files);
      return satellites;
    },
    {} as Record<string, Array<string>>
  );

  if (blazorInAppshell) {
    console.log(
      "The app shell already integrates `piral-blazor` with `blazor`."
    );

    const appShellManifest: BlazorManifest = require(bbAppShellPath);
    const appshellDotnetVersion = extractDotnetVersion(appShellManifest);
    const existingFiles = toFramework(readdirSync(appFrameworkDir));
    const ignored = [...alwaysIgnored, ...existingFiles];

    const [dlls, pdbs] = diffBlazorBootFiles(
      appdir,
      instanceName,
      piletManifest,
      appShellManifest
    );

    checkDotnetVersion(piletDotnetVersion, appshellDotnetVersion);

    copyAll(ignored, staticAssets, targetDir);

    return { dlls, pdbs, standalone, manifest, satellites };
  } else {
    const blazorVersion =
      findBlazorVersion(piralPiletFolder) ||
      getBlazorRelease(piletDotnetVersion);

    console.log(
      "The app shell does not contain `piral-blazor`. Using standalone mode."
    );

    await checkInstallation(blazorVersion, shellPackagePath);

    const originalManifest: BlazorManifest = require(bbStandalonePath);
    const frameworkFiles = toFramework([
      bbjson,
      ...Object.keys(originalManifest.resources.assembly || {}),
      ...Object.keys(originalManifest.resources.pdb || {}),
      ...Object.keys(originalManifest.resources.runtime || {}),
    ]);
    const ignored = [...alwaysIgnored, ...frameworkFiles];

    const [dlls, pdbs] = diffBlazorBootFiles(
      appdir,
      instanceName,
      piletManifest,
      originalManifest
    );

    copyAll(ignored, staticAssets, targetDir);

    return { dlls, pdbs, standalone, manifest, satellites };
  }
}
