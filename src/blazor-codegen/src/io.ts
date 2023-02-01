import { copyFileSync, mkdirSync } from "fs";
import { basename, dirname, resolve } from "path";
import { StaticAsset, StaticAssets } from "./types";

function copyFiles(assets: Array<StaticAsset>, target: string) {
  const watchPaths: Array<string> = [];

  for (const asset of assets) {
    const fromPath = asset.Identity;
    const toPath = resolve(target, getAssetPath(asset));
    const toDir = dirname(toPath);

    mkdirSync(toDir, { recursive: true });
    copyFileSync(fromPath, toPath);
    watchPaths.push(fromPath);
  }

  return watchPaths;
}

export function isAsset(asset: StaticAsset, name: string) {
  return basename(asset.RelativePath) === name;
}

export function getAssetPath(asset: StaticAsset) {
  return asset.BasePath !== "/"
    ? `${asset.BasePath}/${asset.RelativePath}`
    : asset.RelativePath;
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
