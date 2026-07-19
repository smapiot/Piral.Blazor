import { resolve, join, basename } from "path";
import { readFile } from "fs/promises";

import { checkExists, copyAll, getAssetName, loadJson } from "./io";
import { findAppDir } from "./piral";
import { checkInstallation } from "./project";
import { matchesSatellite } from "./utils";
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
  ProjectAssets,
  ProjectConfig,
  StaticAsset,
  StaticAssets,
} from "./types";

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

function getBasicProps(asset: StaticAsset, targetDir: string) {
  const fingerprint = asset.Fingerprint ? `.${asset.Fingerprint}` : "";
  const file = asset.RelativePath.trim()
    .replace("#[.{fingerprint}]?", fingerprint)
    .replace("#[.{fingerprint}]!", fingerprint);

  return {
    id: basename(asset.Identity).replace(fingerprint, ""),
    name: basename(file),
    fingerprint,
    source: asset.Identity,
    target: join(targetDir, file),
  };
}

function getAssets(
  targetDir: string,
  manifest: BlazorManifest,
  staticAssets: StaticAssets,
  projectAssets: ProjectAssets,
): DerivedAssets {
  const assemblies: DerivedAssets["assemblies"] = [];
  const mainProjectName = projectAssets.project.restore.projectName;
  const files: DerivedAssets["files"] = [];
  const symbols: DerivedAssets["symbols"] = [];
  const satellites: DerivedAssets["satellites"] = [];
  const {
    satelliteResources = {},
    fingerprinting = {},
    assembly = {},
    pdb = {},
  } = manifest.resources;

  Object.entries(assembly).forEach(([fullName]) => {
    const originalName = fingerprinting[fullName] || fullName;
    const ext = originalName.endsWith(".dll") ? ".dll" : ".wasm";
    const isEntry = originalName === `${mainProjectName}${ext}`;
    const asset = staticAssets.Assets.find((a) =>
      a.Identity.endsWith(fullName),
    );

    if (asset) {
      assemblies.push({
        ...getBasicProps(asset, targetDir),
        dependency: !isEntry,
        entry: isEntry,
        ignored: false,
      });
    }
  });

  Object.entries(pdb).forEach(([fullName]) => {
    const originalName = fingerprinting[fullName] || fullName;
    const isEntry = originalName === `${mainProjectName}.pdb`;
    const asset = staticAssets.Assets.find(
      (a) => a.AssetTraitValue === "symbol" && a.Identity.endsWith(fullName),
    );

    if (asset) {
      symbols.push({
        ...getBasicProps(asset, targetDir),
        entry: isEntry,
        ignored: false,
      });
    }
  });

  Object.entries(satelliteResources).forEach(([culture, resources]) => {
    const files = Object.keys(resources);
    const findSatelliteAsset = (file: string) =>
      staticAssets.Assets.find((m) => matchesSatellite(m, culture, file));

    files.map(findSatelliteAsset).forEach((asset) => {
      if (asset) {
        satellites.push({
          ...getBasicProps(asset, targetDir),
          culture,
        });
      }
    });
    return satellites;
  });

  staticAssets.Assets.forEach((asset) => {
    if (asset.AssetTraitName === "Content-Encoding") {
      // Empty on purpose
    } else if (asset.AssetTraitValue === "ProjectBundle") {
      // Empty on purpose
    } else if (!asset.RelativePath.startsWith("_framework")) {
      const isCss = asset.RelativePath.endsWith(".css");
      const props = getBasicProps(asset, targetDir);

      if (!alwaysIgnored.includes(props.id)) {
        files.push({
          ...props,
          type: isCss ? "css" : "other",
        });
      }
    }
  });

  return {
    assemblies,
    symbols,
    files,
    satellites,
  };
}

function updateAssets(assets: DerivedAssets, parent: BlazorManifest) {
  const { fingerprinting = {}, assembly = {}, pdb = {} } = parent.resources;

  Object.entries(assembly).forEach(([fullName]) => {
    const originalName = fingerprinting[fullName] || fullName;
    const asset = assets.assemblies.find((m) => m.id === originalName);

    if (asset) {
      asset.ignored = true;
    }
  });

  Object.entries(pdb).forEach(([fullName]) => {
    const originalName = fingerprinting[fullName] || fullName;
    const asset = assets.symbols.find((m) => m.id === originalName);

    if (asset) {
      asset.ignored = true;
    }
  });
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
  const assets = getAssets(
    targetDir,
    piletManifest,
    staticAssets,
    projectAssets,
  );

  if (blazorInAppshell) {
    console.log(
      "The app shell already integrates `piral-blazor` with `blazor`.",
    );

    const appShellManifest = await loadJson<BlazorManifest>(bbAppShellPath);
    const appshellDotnetVersion = extractDotnetVersion(
      appShellManifest,
      projectAssets,
    );

    updateAssets(assets, appShellManifest);
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
    updateAssets(assets, originalManifest);
  }

  const watchlist = [
    config.swaFile,
    config.paFile,
    manifest,
    ...assets.assemblies.filter((m) => !m.ignored).map((m) => m.source),
    ...assets.files.map((m) => m.source),
    ...assets.satellites.map((m) => m.source),
  ].filter((m) => m.indexOf(`/${config.projectName}.`) !== -1);

  await copyAll(assets);

  return {
    watchlist,
    standalone,
    assets,
  };
}
