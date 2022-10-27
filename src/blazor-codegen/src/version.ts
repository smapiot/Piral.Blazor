import { BlazorManifest } from "./types";

/* 
  More advanced version compare that can handle versions 
  like '6.0.1.89w2uv5kng' vs '6.0.1.qyg28onfw5' -> converts to 6.0.1.0 and compares them number by number
  but only compare the first 2 numbers, major and minor versions, ignore patch versions and so on
  */
function isVersionSame(parentVersion: string, childVersion: string) {
  // version looks like 6.0.3.h5gi5nwtz2
  const pp = parentVersion.split(".");
  const cp = childVersion.split(".");
  const len = Math.min(4, Math.max(pp.length, cp.length));

  for (let i = 0; i < Math.min(2, len); i++) {
    const child = cp[i];
    const parent = pp[i];

    if (child !== parent) {
      return "incompatible";
    }
  }

  for (let i = 2; i < Math.min(3, len); i++) {
    const child = cp[i];
    const parent = pp[i];

    // we still match if, e.g.,
    //   parentVersion is 6.0.3... (parent = 3) and
    //   childVersion is 6.0.2... (child = 2)
    // otherwise it might/should still be compatible
    if (child < parent) {
      return "compatible";
    }
  }

  return "match";
}

export function stripVersion(x: string) {
  return x.split("/")[0];
}

export function extractDotnetVersion(manifest: BlazorManifest) {
  return (
    Object.keys(manifest.resources.runtime)
      .map((x) => x.match(/^dotnet\.(.*?)\.js/))
      .find(Boolean)?.[1] || "0.0.0"
  );
}

export function checkDotnetVersion(
  piletDotnetVersion: string,
  appshellDotnetVersion: string
) {
  const versionMatch = isVersionSame(appshellDotnetVersion, piletDotnetVersion);

  if (versionMatch === "incompatible") {
    throw new Error(`The dotnet versions of your pilet and Piral Instance are incompatible:
     - Piral Instance dotnet version = ${appshellDotnetVersion}
     - Pilet dotnet version = ${piletDotnetVersion}`);
  } else if (versionMatch === "compatible") {
    console.warn(`The dotnet versions of your pilet and Piral Instance do not match, but seem to be compatible:
      - Piral Instance dotnet version = ${appshellDotnetVersion}
      - Pilet dotnet version = ${piletDotnetVersion}`);
  }
}
