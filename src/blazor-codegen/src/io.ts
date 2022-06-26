import { copyFileSync, mkdirSync } from "fs";
import { dirname, resolve } from "path";
import { StaticAsset, StaticAssets } from "./types";

function copyFiles(assets: Array<StaticAsset>, target: string) {
  for (const asset of assets) {
    const fromPath = asset.Identity;
    const toPath = resolve(
      target,
      asset.BasePath !== "/"
        ? `${asset.BasePath}/${asset.RelativePath}`
        : asset.RelativePath
    );
    const toDir = dirname(toPath);

    mkdirSync(toDir, { recursive: true });
    copyFileSync(fromPath, toPath);
  }
}

export function copyAll(
  dllFiles: Array<string>,
  pdbFiles: Array<string>,
  ignoredFiles: Array<string>,
  source: StaticAssets,
  targetDir: string
) {
  const staticFiles = source.Assets.map((asset) => ({
    ...asset,
    IsDll: dllFiles.includes(asset.RelativePath),
    IsPdb: pdbFiles.includes(asset.RelativePath),
  })).filter(
    (asset) =>
      asset.IsDll || asset.IsPdb || !ignoredFiles.includes(asset.RelativePath)
  );

  //File copy
  copyFiles(staticFiles, targetDir);
}
