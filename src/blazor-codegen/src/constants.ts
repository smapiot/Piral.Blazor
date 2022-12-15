// filled in by MSBuild
export const targetFramework = "**MSBUILD_TargetFramework**";
export const targetFrameworkAlt = "**MSBUILD_TargetFrameworkMoniker**";
export const configFolderName = "**MSBUILD_ConfigFolder**";
export const blazorprojectfolder = "**MSBUILD_ProjectFolder**";

// dependent on the NODE_ENV variable set by piral cli
export const isRelease = process.env.NODE_ENV === "production";
export const configuration = isRelease ? "Release" : "Debug";
export const action = isRelease ? "publish" : "build";
export const variant = isRelease ? "release" : "debug";

export const blazorrc = ".blazorrc";
export const bbjson = "blazor.boot.json";
export const pajson = "project.assets.json";
export const packageJsonFilename = "package.json";
export const piletJsonFilename = "pilet.json";
export const analyzer = "Piral.Blazor.Analyzer";
export const setupfile = "setup.tsx";
export const teardownfile = "teardown.tsx";
export const swajson = `staticwebassets.${action}.json`;

export const alwaysIgnored = [
  "index.html",
  "favicon.ico",
  "icon-192.png",
];
