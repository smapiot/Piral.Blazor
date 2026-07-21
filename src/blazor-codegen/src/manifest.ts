import { dirname, resolve } from "path";
import { bbjson, dotnetjs, swajson, wasmResourceTraitNames } from "./constants";
import { checkExists, getAssetName, loadJson } from "./io";
import type {
  BlazorJsonManifest,
  BlazorRuntimeManifest,
  StaticAssets,
} from "./types";

async function loadManifestFromDotnetJsPath(path: string) {
  const mod = await import(path);

  if (!mod) {
    return undefined;
  }

  const config = {
    current: undefined as undefined | BlazorRuntimeManifest,
  };

  mod.dotnet.withOnConfigLoaded((cfg: any) => {
    config.current = structuredClone(cfg);
    // Optional: throw to stop early if you only need config
    throw new Error("Stop after config capture");
  });

  try {
    await mod.dotnet.create();
  } catch {}

  return config.current;
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

export async function loadManifestFrom(path: string) {
  const originalManifestExists = await checkExists(path);

  if (originalManifestExists) {
    return await loadJson<BlazorJsonManifest>(path);
  }

  const jsManifestPath = resolve(dirname(path), dotnetjs);
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
    `Could not find the manifest in the specified path (${path}). Something seems to be wrong.`,
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
