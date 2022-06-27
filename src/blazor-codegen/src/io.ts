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
  forcedFiles: Array<string>,
  ignoredFiles: Array<string>,
  source: StaticAssets,
  targetDir: string
) {
  const staticFiles = source.Assets.filter(
    //Either we require the file or it is not ignored -> then we keep it.
    (asset) =>
      forcedFiles.includes(asset.RelativePath) ||
      !ignoredFiles.includes(asset.RelativePath)
  );

  //File copy
  copyFiles(staticFiles, targetDir);
}
