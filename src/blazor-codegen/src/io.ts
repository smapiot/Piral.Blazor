import { copyFileSync, mkdirSync } from "fs";
import { basename, dirname, resolve } from "path";
import { StaticAsset, StaticAssets } from "./types";
import { ignoredAssets } from "./constants";

function isIgnored(path: string) {
  const name = basename(path);

  for (const asset of ignoredAssets) {
    if (asset.test(name)) {
      return true;
    }
  }

  return false;
}

function copyFiles(assets: Array<StaticAsset>, target: string) {
  const watchPaths: Array<string> = [];

  for (const asset of assets) {
    const fromPath = asset.Identity;
    const toPath = resolve(target, getAssetPath(asset));

    // do not copy unnecessary files ...
    if (!isCompressFile(toPath) && !isIgnored(toPath)) {
      const toDir = dirname(toPath);

      mkdirSync(toDir, { recursive: true });
      copyFileSync(fromPath, toPath);
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
  return asset.BasePath !== "/"
    ? `${asset.BasePath}/${name}`
    : name;
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
  targetDir: string
) {
  const staticFiles = source.Assets.filter(
    (asset) => !ignored.includes(getAssetPath(asset))
  );

  //File copy
  return copyFiles(staticFiles, targetDir);
}
