import { resolve, join, basename } from "path";
import { readFile } from "fs/promises";

import { checkExists, copyAll, loadJson } from "./io";
import { findAppDir } from "./piral";
import { checkInstallation } from "./project";
import { matchesSatellite } from "./utils";
import { loadManifestFrom, loadManifestOf } from "./manifest";
import { checkDotnetVersion, extractDotnetVersion } from "./version";
import {
  alwaysIgnored,
  bbjson,
  packageJsonFilename,
  piletJsonFilename,
  variant,
  blazorrc,
} from "./constants";
import type {
  BlazorJsonManifest,
  BlazorRuntimeManifest,
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

function getAssetPath(asset: StaticAsset, name: string) {
  return asset.BasePath !== "/" ? `${asset.BasePath}/${name}` : name;
}

function getBasicProps(asset: StaticAsset, targetDir: string) {
  const fingerprint = asset.Fingerprint ? `.${asset.Fingerprint}` : "";
  const replacement =
    asset.OriginalItemSpec.startsWith("wwwroot") ||
    asset.OriginalItemSpec.endsWith(".map")
      ? ""
      : fingerprint;
  const file = asset.RelativePath.trim()
    .replace("#[.{fingerprint}]?", replacement)
    .replace("#[.{fingerprint}]!", replacement);

  return {
    id: basename(asset.Identity).replace(fingerprint, ""),
    name: getAssetPath(asset, file),
    fingerprint,
    source: asset.Identity,
    target: join(targetDir, file),
  };
}

function getAssets(
  targetDir: string,
  manifest: BlazorJsonManifest | BlazorRuntimeManifest,
  staticAssets: StaticAssets,
  projectAssets: ProjectAssets,
): DerivedAssets {
  const byIntegrity = new Map(
    staticAssets.Assets.map((a) => [`sha256-${a.Integrity}`, a]),
  );
  const assemblies: DerivedAssets["assemblies"] = [];
  const mainProjectName = projectAssets.project.restore.projectName;
  const files: DerivedAssets["files"] = [];
  const symbols: DerivedAssets["symbols"] = [];
  const satellites: DerivedAssets["satellites"] = [];

  if ("extensions" in manifest) {
    const {
      assembly = [],
      pdb = [],
      satelliteResources = {},
    } = manifest.resources;

    assembly.forEach((entry) => {
      const originalName = entry.virtualPath;
      const ext = originalName.endsWith(".dll") ? ".dll" : ".wasm";
      const isEntry = originalName === `${mainProjectName}${ext}`;
      const asset = byIntegrity.get(entry.integrity);

      if (asset) {
        assemblies.push({
          ...getBasicProps(asset, targetDir),
          dependency: !isEntry,
          entry: isEntry,
          ignored: false,
        });
      }
    });

    pdb.forEach((entry) => {
      const originalName = entry.virtualPath;
      const isEntry = originalName === `${mainProjectName}.pdb`;
      const asset = byIntegrity.get(entry.integrity);

      if (asset) {
        symbols.push({
          ...getBasicProps(asset, targetDir),
          entry: isEntry,
          ignored: false,
        });
      }
    });

    Object.entries(satelliteResources).forEach(([culture, resources]) => {
      resources
        .map((entry) =>
          staticAssets.Assets.find((m) =>
            matchesSatellite(m, culture, entry.virtualPath),
          ),
        )
        .forEach((asset) => {
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
        const isCss = asset.RelativePath.endsWith(".css");
        const props = getBasicProps(asset, join(targetDir, asset.BasePath));
        files.push({
          ...props,
          type: isCss ? "css" : "other",
        });
      } else if (
        asset.AssetTraitValue === "" &&
        asset.Identity.endsWith(".map")
      ) {
        const props = getBasicProps(asset, join(targetDir, asset.BasePath));
        files.push({
          ...props,
          type: "other",
        });
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
  } else {
    const {
      satelliteResources = {},
      fingerprinting = {},
      assembly = {},
      pdb = {},
    } = manifest.resources;

    Object.entries(assembly).forEach(([fullName, integrity]) => {
      const originalName = fingerprinting[fullName] || fullName;
      const ext = originalName.endsWith(".dll") ? ".dll" : ".wasm";
      const isEntry = originalName === `${mainProjectName}${ext}`;
      const asset = byIntegrity.get(integrity);

      if (asset) {
        assemblies.push({
          ...getBasicProps(asset, targetDir),
          dependency: !isEntry,
          entry: isEntry,
          ignored: false,
        });
      }
    });

    Object.entries(pdb).forEach(([fullName, integrity]) => {
      const originalName = fingerprinting[fullName] || fullName;
      const isEntry = originalName === `${mainProjectName}.pdb`;
      const asset = byIntegrity.get(integrity);

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
        staticAssets.Assets.find((m) => matchesSatellite(m, culture, file)); //TODO

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
        const isCss = asset.RelativePath.endsWith(".css");
        const props = getBasicProps(asset, join(targetDir, asset.BasePath));
        files.push({
          ...props,
          type: isCss ? "css" : "other",
        });
      } else if (
        asset.AssetTraitValue === "" &&
        asset.Identity.endsWith(".map")
      ) {
        const props = getBasicProps(asset, join(targetDir, asset.BasePath));
        files.push({
          ...props,
          type: "other",
        });
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
}

function updateAssets(
  assets: DerivedAssets,
  parent: BlazorJsonManifest | BlazorRuntimeManifest,
) {
  if ("extensions" in parent) {
    const { assembly = [], pdb = [] } = parent.resources;

    assembly.forEach((entry) => {
      const originalName = entry.virtualPath;
      const asset = assets.assemblies.find((m) => m.id === originalName);

      if (asset) {
        asset.ignored = true;
      }
    });

    pdb.forEach((entry) => {
      const originalName = entry.virtualPath;
      const asset = assets.symbols.find((m) => m.id === originalName);

      if (asset) {
        asset.ignored = true;
      }
    });
  } else {
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
}

export async function prepare(targetDir: string, config: ProjectConfig) {
  // Require modules
  const projectAssets = await loadJson<ProjectAssets>(config.paFile);
  const staticAssets = await loadJson<StaticAssets>(config.swaFile);
  const piralPiletFolder = resolve(__dirname, "..");
  const instanceName = await findInstanceName(piralPiletFolder);
  const appdir = await findAppDir(piralPiletFolder, instanceName);

  // Piral Blazor checks
  const appFrameworkDir = resolve(appdir, "app", "_framework");
  const bbAppShellPath = resolve(appFrameworkDir, bbjson);
  const blazorInAppshell = await checkExists(bbAppShellPath);
  const shellPackagePath = resolve(appdir, packageJsonFilename);
  const [manifest, piletManifest] = await loadManifestOf(staticAssets);
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

    const appShellManifest = await loadManifestFrom(bbAppShellPath);
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
    const originalManifest = await loadManifestFrom(manifestPath);
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
