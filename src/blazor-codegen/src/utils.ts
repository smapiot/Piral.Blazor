import { checkExists, loadJson } from "./io";
import type { ProjectConfig, StaticAsset, StaticAssets } from "./types";

function matchesIdentity(asset: StaticAsset, file: string) {
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
    asset.AssetTraitName === "Culture" &&
    asset.AssetTraitValue === culture &&
    matchesIdentity(asset, file)
  );
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
