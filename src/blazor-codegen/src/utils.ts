import { checkExists, loadJson } from "./io";
import { ignoredDlls } from "./constants";
import type {
  BlazorManifest,
  BlazorResourceType,
  ProjectConfig,
  StaticAsset,
  StaticAssets,
} from "./types";

function getAllKeys(manifest: BlazorManifest, type: BlazorResourceType) {
  return Object.keys(manifest.resources[type] || {});
}

export function matchesIdentity(asset: StaticAsset, file: string) {
  return (
    asset.Identity.endsWith(`/${file}`) || asset.Identity.endsWith(`\\${file}`)
  );
}

export function matchesSatellite(
  asset: StaticAsset,
  culture: string,
  file: string,
) {
  return (
    asset.AssetRole === "Related" &&
    asset.AssetTraitValue === culture &&
    matchesIdentity(asset, file)
  );
}

function getUniqueKeys(
  originalManifest: BlazorManifest,
  piletManifest: BlazorManifest,
  type: BlazorResourceType,
) {
  const original = getAllKeys(originalManifest, type);
  const dedicated = getAllKeys(piletManifest, type);
  return dedicated.filter(
    (m) => !original.includes(m) && !ignoredDlls.includes(m),
  );
}

export function getRef(assemblies: Array<string>, name: string) {
  const fingerprint = /^[0-9a-z]{10}$/;
  const prefix = `${name}.`;

  for (const dll of assemblies) {
    if (
      dll.startsWith(prefix) &&
      (dll.endsWith(".wasm") || dll.endsWith(".dll"))
    ) {
      const segments = dll.substring(prefix.length).split(".");
      segments.pop();

      if (
        segments.length === 0 ||
        (segments.length === 1 && fingerprint.test(segments[0]))
      ) {
        return dll;
      }
    }
  }

  return name;
}

export async function rebuildNeeded(config: ProjectConfig) {
  const paExists = await checkExists(config.paFile);
  const swaExists = await checkExists(config.swaFile);

  if (paExists && swaExists) {
    const staticAssets = await loadJson<StaticAssets>(config.swaFile);

    for (const asset of staticAssets.Assets) {
      const exists = await checkExists(asset.Identity);

      if (!exists) {
        return true;
      }
    }

    return false;
  }

  return true;
}

export async function diffBlazorBootFiles(
  appdir: string,
  appname: string,
  piletManifest: BlazorManifest,
  originalManifest: BlazorManifest,
): Promise<[Array<string>, Array<string>]> {
  const appDirExists = await checkExists(appdir);

  if (!appDirExists) {
    throw new Error(
      `Cannot find the directory of "${appname}". Please re-install the dependencies.`,
    );
  }

  return [
    getUniqueKeys(originalManifest, piletManifest, "assembly"),
    getUniqueKeys(originalManifest, piletManifest, "pdb"),
  ];
}
