import { BlazorManifest } from "./types";

export function extractBlazorVersion(manifest: BlazorManifest) {
  return (
    Object.keys(manifest.resources.runtime)
      .map((x) => x.match(/^dotnet\.(.*?)\.js/))
      .find(Boolean)?.[1] || "0.0.0"
  );
}

/* 
  More advanced version compare that can handle versions 
  like '6.0.1.89w2uv5kng' vs '6.0.1.qyg28onfw5' -> converts to 6.0.1.0 and compares them number by number
  but only compare the first 2 numbers, major and minor versions, ignore patch versions and so on
  */
function isVersionSame(oldVer: string, newVer: string) {
  // version looks like 6.0.3.h5gi5nwtz2
  const oldParts = oldVer.split(".");
  const newParts = newVer.split(".");
  const len = Math.min(4, Math.max(oldParts.length, newParts.length));

  for (let i = 0; i < Math.min(2, len); i++) {
    const a = newParts[i];
    const b = oldParts[i];

    if (a !== b) {
      return "incompatible";
    }
  }

  for (let i = 2; i < Math.min(4, len); i++) {
    const a = newParts[i];
    const b = oldParts[i];

    if (a !== b) {
      return "compatible";
    }
  }

  return "match";
}

export function checkBlazorVersion(
  piletBlazorVersion: string,
  appshellBlazorVersion: string
) {
  const versionMatch = isVersionSame(appshellBlazorVersion, piletBlazorVersion);

  if (versionMatch === "incompatible") {
    throw new Error(`The Blazor versions of your pilet and Piral Instance are incompatible:
     - Piral Instance Blazor version = ${appshellBlazorVersion}
     - Pilet Blazor version = ${piletBlazorVersion}`);
  } else if (versionMatch === "compatible") {
    console.warn(`The Blazor versions of your pilet and Piral Instance do not match, but seem to be compatible:
      - Piral Instance Blazor version = ${appshellBlazorVersion}
      - Pilet Blazor version = ${piletBlazorVersion}`);
  }
}
