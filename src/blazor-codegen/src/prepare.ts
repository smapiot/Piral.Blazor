import { resolve, join } from "path";
import { existsSync, readdirSync } from "fs";
import { copyAll } from "./io";
import { findAppDir } from "./piral";
import { checkInstallation } from "./project";
import { diffBlazorBootFiles } from "./utils";
import { checkBlazorVersion, extractBlazorVersion } from "./version";
import { alwaysIgnored, bbjson, swajson, packageJsonFilename, piletJsonFilename, variant } from "./constants";
import { BlazorManifest, StaticAssets } from "./types";

function toFramework(files: Array<string>) {
  return files.map((n) => `_framework/${n}`);
}

export async function prepare(targetDir: string, staticAssets: StaticAssets) {
  const piralPiletFolder = resolve(__dirname, "..");
  const packageJson = require(resolve(piralPiletFolder, packageJsonFilename));

  const piletJsonFilePath = join(piralPiletFolder, piletJsonFilename).replace(/\\/g, "/");
  const piletJsonFileExists = existsSync(piletJsonFilePath);
  let instanceName;
  if(piletJsonFileExists){
    const piletJson = require(resolve(piralPiletFolder, piletJsonFilename));
    const selectedInstance = Object.keys(piletJson.piralInstances).find(key => piletJson.piralInstances[key].selected);
    if(selectedInstance !== undefined){
      instanceName = selectedInstance;
    } else{
      instanceName = Object.keys(piletJson.piralInstances)[0];
    }
  } else{
    instanceName = packageJson.piral.name;
  }


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
  const piletBlazorVersion = extractBlazorVersion(piletManifest);
  const standalone = !blazorInAppshell;

  if (blazorInAppshell) {
    console.log(
      "The app shell already integrates `piral-blazor` with `blazor`."
    );

    const appShellManifest: BlazorManifest = require(bbAppShellPath);
    const appshellBlazorVersion = extractBlazorVersion(appShellManifest);
    const existingFiles = toFramework(readdirSync(appFrameworkDir));
    const ignored = [...alwaysIgnored, ...existingFiles];

    const [dlls, pdbs] = diffBlazorBootFiles(
      appdir,
      instanceName,
      piletManifest,
      appShellManifest
    );

    checkBlazorVersion(piletBlazorVersion, appshellBlazorVersion);

    copyAll(ignored, staticAssets, targetDir);

    return { dlls, pdbs, standalone, manifest };
  } else {
    console.log(
      "The app shell does not contain `piral-blazor`. Using standalone mode."
    );

    await checkInstallation(piletBlazorVersion, shellPackagePath);

    const originalManifest: BlazorManifest = require(bbStandalonePath);
    const frameworkFiles = toFramework([
      bbjson,
      ...Object.keys(originalManifest.resources.assembly),
      ...Object.keys(originalManifest.resources.pdb),
      ...Object.keys(originalManifest.resources.runtime),
    ]);
    const ignored = [...alwaysIgnored, ...frameworkFiles];

    const [dlls, pdbs] = diffBlazorBootFiles(
      appdir,
      instanceName,
      piletManifest,
      originalManifest
    );

    copyAll(ignored, staticAssets, targetDir);

    return { dlls, pdbs, standalone, manifest };
  }
}
