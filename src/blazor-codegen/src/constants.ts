// filled in by MSBuild
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

const ignoredNames = [
  "Microsoft.Build.Framework",
  "Microsoft.Build.Utilities.Core",
  "Microsoft.NET.StringTools",
  "Microsoft.Win32.SystemEvents",
  "Piral.Blazor.Tools",
  "System.Configuration.ConfigurationManager",
  "System.Drawing.Common",
  "System.Security.Cryptography.ProtectedData",
  "System.Security.Permissions",
  "System.Windows.Extensions",
];

export const ignoredDlls = [
  ...ignoredNames.map((n) => `${n}.dll`),
  ...ignoredNames.map((n) => `${n}.wasm`),
];

export const wasmResourceTraitNames = [
  "BlazorWebAssemblyResource",
  "WasmResource",
];
export const scopedCssTraitNames = ["ScopedCss"];
export const ignoredAssets = [/^dotnet\..*\.js$/];
export const alwaysIgnored = ["index.html", "favicon.ico", "icon-192.png"];
