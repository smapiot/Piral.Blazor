import { resolve, join } from "path";
import { readdir, readFile } from "fs/promises";

import { checkExists, copyAll, getAssetName, loadJson } from "./io";
import { findAppDir } from "./piral";
import { checkInstallation } from "./project";
import { diffBlazorBootFiles, matchesSatellite } from "./utils";
import { checkDotnetVersion, extractDotnetVersion } from "./version";
import {
  alwaysIgnored,
  bbjson,
  swajson,
  packageJsonFilename,
  piletJsonFilename,
  wasmResourceTraitNames,
  variant,
  blazorrc,
} from "./constants";
import type {
  BlazorManifest,
  DerivedAssets,
  NameMapper,
  ProjectAssets,
  ProjectConfig,
  SatelliteAssets,
  StaticAssets,
} from "./types";

function toFramework(files: Array<string>) {
  return files.map((n) => `_framework/${n}`);
}

async function findInstanceName(piralPiletFolder: string): Promise<string> {
  const packageJson = await loadJson(
    resolve(piralPiletFolder, packageJsonFilename),
  );
  const piletJsonFilePath = join(piralPiletFolder, piletJsonFilename).replace(
    /\\/g,
    "/",
  );
  const piletJsonFileExists = await checkExists(piletJsonFilePath);

  if (piletJsonFileExists) {
    const piletJson = await loadJson(
      resolve(piralPiletFolder, piletJsonFilename),
    );
    const selectedInstance = Object.keys(piletJson.piralInstances).find(
      (key) => piletJson.piralInstances[key].selected,
    );

    if (selectedInstance !== undefined) {
      return selectedInstance;
    }

    return Object.keys(piletJson.piralInstances)[0];
  }

  return packageJson.piral.name;
}

async function findBlazorVersion(piralPiletFolder: string) {
  const key = "Version=";
  const blazorrcPath = resolve(piralPiletFolder, blazorrc);
  const content = await readFile(blazorrcPath, "utf8");
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
      "Could not detect version of Blazor. Something does not seem right.",
    );
  }

  const [npmBlazorVersion] = result;
  const [blazorRelease] = npmBlazorVersion.split(".");
  return `^${blazorRelease}`;
}

function getAssets(
  config: ProjectConfig,
  manifest: BlazorManifest,
  staticAssets: StaticAssets,
  projectAssets: ProjectAssets,
): DerivedAssets {
  const assemblies: DerivedAssets["assemblies"] = [];
  const files: DerivedAssets["files"] = [];
  const { satelliteResources, fingerprinting = {} } = manifest.resources;

  const satellites: DerivedAssets["satellites"] = Object.keys(
    satelliteResources || {},
  ).reduce((satellites, name) => {
    const resources = satelliteResources[name];
    const files = Object.keys(resources);
    const toSatellitePath = (file: string) =>
      staticAssets.Assets.find((m) => matchesSatellite(m, name, file))
        ?.RelativePath;
    satellites[name] = files
      .map(toSatellitePath)
      .filter(Boolean)
      .map((file) => ({
        name: "",
        source: "",
        target: "",
      }));
    return satellites;
  }, {} as SatelliteAssets);

  return {
    assemblies,
    files,
    satellites,
  };
}

export async function prepare(targetDir: string, config: ProjectConfig) {
  // Require modules
  const projectAssets = await loadJson<ProjectAssets>(config.paFile);
  const staticAssets = await loadJson<StaticAssets>(config.swaFile);
  const piralPiletFolder = resolve(__dirname, "..");
  const instanceName = await findInstanceName(piralPiletFolder);
  const appdir = await findAppDir(piralPiletFolder, instanceName);

  const manifestSource = staticAssets.Assets.find(
    (m) =>
      wasmResourceTraitNames.includes(m.AssetTraitName) &&
      m.AssetTraitValue === "manifest" &&
      getAssetName(m).endsWith(bbjson),
  );

  if (!manifestSource) {
    throw new Error(
      `Could not find the "${bbjson}" in ${swajson}. Something seems to be wrong.`,
    );
  }

  // Piral Blazor checks
  const appFrameworkDir = resolve(appdir, "app", "_framework");
  const bbAppShellPath = resolve(appFrameworkDir, bbjson);
  const blazorInAppshell = await checkExists(bbAppShellPath);
  const shellPackagePath = resolve(appdir, packageJsonFilename);
  const manifest = manifestSource.Identity;
  const piletManifest = await loadJson<BlazorManifest>(manifest);
  const piletDotnetVersion = extractDotnetVersion(piletManifest, projectAssets);
  const standalone = !blazorInAppshell;
  const { satelliteResources, fingerprinting = {} } = piletManifest.resources;
  const nameToFingerprint = new Map(
    Object.entries(fingerprinting).map(([key, value]) => [value, key]),
  );
  const fingerprintToName = new Map(
    Object.entries(fingerprinting).map(([key, value]) => [key, value]),
  );
  const nameMapping: NameMapper = {
    toName(fingerprint: string) {
      return fingerprintToName.get(fingerprint) ?? fingerprint;
    },
    toFingerprint(name: string) {
      return nameToFingerprint.get(name) ?? name;
    },
  };

  if (blazorInAppshell) {
    console.log(
      "The app shell already integrates `piral-blazor` with `blazor`.",
    );

    const appShellManifest = await loadJson<BlazorManifest>(bbAppShellPath);
    const appshellDotnetVersion = extractDotnetVersion(
      appShellManifest,
      projectAssets,
    );
    const appFrameworkFiles = await readdir(appFrameworkDir);
    const existingFiles = toFramework(appFrameworkFiles);
    const ignored = [...alwaysIgnored, ...existingFiles];

    const [dlls, pdbs] = await diffBlazorBootFiles(
      appdir,
      instanceName,
      piletManifest,
      appShellManifest,
    );

    checkDotnetVersion(piletDotnetVersion, appshellDotnetVersion);
  } else {
    const blazorVersion =
      (await findBlazorVersion(piralPiletFolder)) ||
      getBlazorRelease(piletDotnetVersion);

    console.log(
      "The app shell does not contain `piral-blazor`. Using standalone mode.",
    );

    await checkInstallation(blazorVersion, shellPackagePath);

    const bbStandalonePath = `blazor/${variant}/wwwroot/_framework/${bbjson}`;
    const manifestPath = require.resolve(bbStandalonePath);
    const originalManifest = await loadJson<BlazorManifest>(manifestPath);
    const frameworkFiles = toFramework([
      bbjson,
      ...Object.keys(originalManifest.resources.assembly || {}),
      ...Object.keys(originalManifest.resources.pdb || {}),
      ...Object.keys(originalManifest.resources.runtime || {}),
      ...Object.keys(originalManifest.resources.jsModuleRuntime || {}),
      ...Object.keys(originalManifest.resources.jsModuleNative || {}),
      ...Object.keys(originalManifest.resources.wasmNative || {}),
    ]);
    const ignored = [...alwaysIgnored, ...frameworkFiles];

    const [dlls, pdbs] = await diffBlazorBootFiles(
      appdir,
      instanceName,
      piletManifest,
      originalManifest,
    );
  }

  const assets = getAssets(config, piletManifest, staticAssets, projectAssets);
  const watchlist = [
    config.swaFile,
    config.paFile,
    manifest,
    ...assets.assemblies.filter((m) => !m.ignored).map((m) => m.source),
  ].filter((m) => m.indexOf(`/${config.projectName}.`) !== -1);

  return {
    watchlist,
    standalone,
    assets,
  };
}
