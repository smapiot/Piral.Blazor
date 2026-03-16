import { copyFile, mkdir, readFile, stat } from "fs/promises";
import { basename, dirname, resolve } from "path";

import { ignoredAssets } from "./constants";
import type { StaticAsset, StaticAssets } from "./types";

function isIgnored(path: string) {
  const name = basename(path);

  for (const asset of ignoredAssets) {
    if (asset.test(name)) {
      return true;
    }
  }

  return false;
}

async function copyFiles(assets: Array<StaticAsset>, target: string) {
  const watchPaths: Array<string> = [];

  for (const asset of assets) {
    const fromPath = asset.Identity;
    const toPath = resolve(target, getAssetPath(asset));

    // do not copy unnecessary files ...
    if (!isCompressFile(toPath) && !isIgnored(toPath)) {
      const toDir = dirname(toPath);

      await mkdir(toDir, { recursive: true });
      await copyFile(fromPath, toPath);
      watchPaths.push(fromPath);
    }
  }

  return watchPaths;
}

export function isCompressFile(path: string) {
  return path.endsWith(".gz") || path.endsWith(".br");
}

export function getAssetName(asset: StaticAsset) {
  return (
    asset?.RelativePath
      // Handle legacy patterns (both ! and ?)
      .replace(/#\[\.{fingerprint}\][!?]/g, "")
      // Handle .NET {0} placeholder pattern
      .replace(/-\{0\}-[a-zA-Z0-9]+-[a-zA-Z0-9]+/g, "")
  );
}

export function isAsset(asset: StaticAsset, name: string) {
  return basename(asset.Identity) === name;
}

export function getAssetPath(asset: StaticAsset) {
  const name = getAssetName(asset);
  return asset.BasePath !== "/" ? `${asset.BasePath}/${name}` : name;
}

export function getFilePath(source: StaticAssets, name: string) {
  const item = source.Assets.find((m) => isAsset(m, name));

  if (item) {
    return getAssetPath(item);
  }

  return name;
}

export function copyAll(
  ignored: Array<string>,
  source: StaticAssets,
  targetDir: string,
) {
  const staticFiles = source.Assets.filter(
    (asset) => !ignored.includes(getAssetPath(asset)),
  );

  //File copy
  return copyFiles(staticFiles, targetDir);
}

export async function checkExists(fn: string) {
  try {
    await stat(fn);
    return true;
  } catch {
    return false;
  }
}

export async function loadJson(fn: string) {
  const content = await readFile(fn, "utf8");
  return JSON.parse(content);
}
