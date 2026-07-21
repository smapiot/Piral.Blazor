import { resolve } from "path";
import { readFile } from "fs/promises";

import { bbjson, dotnetjs, swajson, wasmResourceTraitNames } from "./constants";
import { checkExists, getAssetName, loadJson } from "./io";
import type {
  BlazorJsonManifest,
  BlazorRuntimeManifest,
  StaticAssets,
} from "./types";

async function loadManifestFromDotnetJsPath(path: string) {
  const src = await readFile(path, "utf8");
  const m = src.match(/\/\*json-start\*\/([\s\S]*?)\/\*json-end\*\//);

  if (!m) return undefined;

  return JSON.parse(m[1]) as BlazorRuntimeManifest;
}

async function loadManifestFromDotnetJs(assets: StaticAssets) {
  const manifestSource = assets.Assets.find(
    (m) =>
      wasmResourceTraitNames.includes(m.AssetTraitName) &&
      m.AssetTraitValue === "manifest" &&
      getAssetName(m).endsWith(dotnetjs),
  );

  if (!manifestSource) {
    return undefined;
  }

  const manifest = manifestSource.Identity;
  const content = await loadManifestFromDotnetJsPath(manifest);

  if (!content) {
    return undefined;
  }

  return [manifest, content] as const;
}

async function loadManifestFromBlazorBootJson(assets: StaticAssets) {
  const manifestSource = assets.Assets.find(
    (m) =>
      wasmResourceTraitNames.includes(m.AssetTraitName) &&
      m.AssetTraitValue === "manifest" &&
      getAssetName(m).endsWith(bbjson),
  );

  if (!manifestSource) {
    return undefined;
  }

  const manifest = manifestSource.Identity;
  const content = await loadJson<BlazorJsonManifest>(manifest);
  return [manifest, content] as const;
}

export async function loadManifestFrom(dir: string) {
  const jsonManifestPath = resolve(dir, bbjson);
  const jsonManifestExists = await checkExists(jsonManifestPath);

  if (jsonManifestExists) {
    return await loadJson<BlazorJsonManifest>(jsonManifestPath);
  }

  const jsManifestPath = resolve(dir, dotnetjs);
  const jsManifestExists = await checkExists(jsManifestPath);

  if (jsManifestExists) {
    const content = await loadManifestFromDotnetJsPath(jsManifestPath);

    if (!content) {
      throw new Error(
        `Could not load the manifest from the specified path (${jsManifestPath}). Something seems to be wrong.`,
      );
    }

    return content;
  }

  throw new Error(
    `Could not find the manifest in the specified path (${dir}). Something seems to be wrong.`,
  );
}

export async function loadManifestOf(assets: StaticAssets) {
  const result =
    (await loadManifestFromBlazorBootJson(assets)) ??
    (await loadManifestFromDotnetJs(assets));

  if (!result) {
    throw new Error(
      `Could not find the manifest in ${swajson}. Something seems to be wrong.`,
    );
  }

  return result;
}
