import { existsSync } from "fs";
import { basename } from "path";
import { BlazorManifest, BlazorResourceType, StaticAssets } from "./types";

function getAllKeys(manifest: BlazorManifest, type: BlazorResourceType) {
  return Object.keys(manifest.resources[type] || {});
}

function getUniqueKeys(
  originalManifest: BlazorManifest,
  piletManifest: BlazorManifest,
  type: BlazorResourceType
) {
  const original = getAllKeys(originalManifest, type);
  const dedicated = getAllKeys(piletManifest, type);
  return dedicated.filter((m) => !original.includes(m));
}

const projExtension = ".csproj";

export function getProjName(x: string) {
  return basename(x).slice(0, -projExtension.length);
}

export function rebuildNeeded(pafile: string, swafile: string) {
  if (existsSync(pafile) && existsSync(swafile)) {
    const staticAssets: StaticAssets = require(swafile);

    if (staticAssets.Assets.every(m => existsSync(m.Identity))) {
      return false;
    }
  }
  
  return true;
}

export function diffBlazorBootFiles(
  appdir: string,
  appname: string,
  piletManifest: BlazorManifest,
  originalManifest: BlazorManifest
): [Array<string>, Array<string>] {
  if (!existsSync(appdir)) {
    throw new Error(
      `Cannot find the directory of "${appname}". Please re-install the dependencies.`
    );
  }

  return [
    getUniqueKeys(originalManifest, piletManifest, "assembly"),
    getUniqueKeys(originalManifest, piletManifest, "pdb"),
  ];
}
