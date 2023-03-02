import { existsSync } from "fs";
import {
  BlazorManifest,
  BlazorResourceType,
  ProjectConfig,
  StaticAssets,
} from "./types";

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

export function rebuildNeeded(config: ProjectConfig) {
  if (existsSync(config.paFile) && existsSync(config.swaFile)) {
    const staticAssets: StaticAssets = require(config.swaFile);

    if (staticAssets.Assets.every((m) => existsSync(m.Identity))) {
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
