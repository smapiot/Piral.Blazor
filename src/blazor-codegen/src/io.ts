import { copyFile, mkdir, readFile, stat } from "fs/promises";
import { dirname } from "path";

import type { DerivedAssets, StaticAsset } from "./types";

export function isCompressFile(path: string) {
  return path.endsWith(".gz") || path.endsWith(".br");
}

export function getAssetName(asset: StaticAsset) {
  return (
    asset?.RelativePath
      // Handle legacy patterns (both ! and ?)
      .replace(/#\[\.\{fingerprint\}\][!?]/g, "")
      // Handle .NET {0} placeholder pattern
      .replace(/-\{0\}-[a-zA-Z0-9]+-[a-zA-Z0-9]+/g, "")
  );
}

export async function copyAll(assets: DerivedAssets) {
  for (const asset of assets.assemblies) {
    if (!asset.ignored) {
      const toDir = dirname(asset.target);
      await mkdir(toDir, { recursive: true });
      await copyFile(asset.source, asset.target);
    }
  }

  for (const asset of assets.files) {
    const toDir = dirname(asset.target);
    await mkdir(toDir, { recursive: true });
    await copyFile(asset.source, asset.target);
  }

  for (const asset of assets.satellites) {
    const toDir = dirname(asset.target);
    await mkdir(toDir, { recursive: true });
    await copyFile(asset.source, asset.target);
  }

  for (const asset of assets.symbols) {
    if (!asset.ignored) {
      const toDir = dirname(asset.target);
      await mkdir(toDir, { recursive: true });
      await copyFile(asset.source, asset.target);
    }
  }
}

export async function checkExists(fn: string) {
  try {
    await stat(fn);
    return true;
  } catch {
    return false;
  }
}

export async function loadJson<T = any>(fn: string) {
  const content = await readFile(fn, "utf8");
  return JSON.parse(content) as T;
}
