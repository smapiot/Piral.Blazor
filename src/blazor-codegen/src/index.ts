import { join, resolve, basename } from "path";
import { existsSync, readdirSync } from "fs";
import { copyAll, getAssetPath, getFilePath } from "./io";
import { findAppDir } from "./piral";
import { diffBlazorBootFiles } from "./utils";
import { createAllTargetRefs } from "./targets";
import { checkBlazorVersion, extractBlazorVersion } from "./version";
import { analyzeProject, buildSolution, checkInstallation } from "./project";
import { BlazorManifest, ProjectAssets, StaticAssets } from "./types";
import {
  fallbackPiletCode,
  makePiletCode,
  makePiletHead,
  standaloneRemapCode,
} from "./snippets";
import {
  pjson,
  configuration,
  targetFramework,
  setupfile,
  isRelease,
  pajson,
  swajson,
  bbjson,
  alwaysIgnored,
  configFolderName,
  teardownfile,
  variant,
} from "./constants";

const piralPiletFolder = resolve(__dirname, "..");
const rootFolder = resolve(piralPiletFolder, "..", "..");
const blazorfolderName = basename(piralPiletFolder);
const blazorprojectfolder = resolve(rootFolder, blazorfolderName);
const piralconfigfolder = resolve(blazorprojectfolder, configFolderName);
const objectsDir = resolve(blazorprojectfolder, "obj");
const pafile = resolve(objectsDir, pajson);
const swafile = resolve(objectsDir, configuration, targetFramework, swajson);

const project = require(resolve(piralPiletFolder, pjson));
const appdir = findAppDir(piralPiletFolder, project.piral.name);

const shellPackagePath = resolve(appdir, pjson);
const appFrameworkDir = resolve(appdir, "app", "_framework");

module.exports = async function () {
  const targetDir = this.options.outDir;

  //always build when files not found or in release
  if (isRelease || !existsSync(pafile) || !existsSync(swafile)) {
    try {
      await buildSolution(blazorprojectfolder);
    } catch (err) {
      throw new Error(
        `Something went wrong with the Blazor build.\n` +
          `Make sure there is at least one Blazor project in your solution.\n` +
          `Seen error: ${err}`
      );
    }
  }

  // Require modules
  const projectAssets: ProjectAssets = require(pafile);
  const staticAssets: StaticAssets = require(swafile);
  const manifestSource = staticAssets.Assets.find(
    (m) =>
      m.AssetRole === "Primary" &&
      m.RelativePath === "_framework/blazor.boot.json"
  );

  if (!manifestSource) {
    throw new Error(
      `Could not find the "blazor.boot.json" in ${swafile}. Something seems to be wrong.`
    );
  }

  const piletManifest: BlazorManifest = require(manifestSource.Identity);

  // Piral Blazor checks
  const bbAppShellPath = resolve(appFrameworkDir, bbjson);
  const bbStandalonePath = `blazor/${variant}/wwwroot/_framework/${bbjson}`;
  const blazorInAppshell = existsSync(bbAppShellPath);
  const piletBlazorVersion = extractBlazorVersion(piletManifest);

  let dlls: Array<string> = [];
  let pdbs: Array<string> = [];

  if (blazorInAppshell) {
    console.log(
      "The app shell already integrates `piral-blazor` with `blazor`."
    );

    const appShellManifest: BlazorManifest = require(bbAppShellPath);
    const appshellBlazorVersion = extractBlazorVersion(appShellManifest);
    const existingFiles = readdirSync(appFrameworkDir).map(
      (n) => `_framework/${n}`
    );
    const ignored = [...alwaysIgnored, ...existingFiles];

    [dlls, pdbs] = diffBlazorBootFiles(
      appdir,
      project.piral.name,
      piletManifest,
      appShellManifest
    );

    checkBlazorVersion(piletBlazorVersion, appshellBlazorVersion);

    copyAll(ignored, staticAssets, targetDir);
  } else {
    console.log(
      "The app shell does not contain `piral-blazor`. Using standalone mode."
    );

    await checkInstallation(piletBlazorVersion, shellPackagePath);

    const originalManifest = require(bbStandalonePath);

    [dlls, pdbs] = diffBlazorBootFiles(
      appdir,
      project.piral.name,
      piletManifest,
      originalManifest
    );

    copyAll(alwaysIgnored, staticAssets, targetDir);
  }

  const allImports: Array<string> = [];

  // Piral Blazor API

  if (!blazorInAppshell) {
    allImports.push(
      `import { defineBlazorReferences, fromBlazor, releaseBlazorReferences } from 'piral-blazor/convert';`
    );
  }

  const getPiralBlazorApiCode = `export function initPiralBlazorApi(app) {
    ${blazorInAppshell ? "" : standaloneRemapCode}
  }`;

  // Refs
  const uniqueDependencies = dlls.map((f) => f.replace(".dll", ""));
  const bundleFiles = staticAssets.Assets.filter(
    (m) => m.AssetTraitValue === "ProjectBundle"
  );
  const cssLinks = bundleFiles
    .filter((m) => m.AssetTraitName === "ScopedCss")
    .map(getAssetPath);
  const refs = createAllTargetRefs(uniqueDependencies, projectAssets);
  const files = [...refs.map((ref) => `${ref}.dll`), ...pdbs].map((name) =>
    getFilePath(staticAssets, name)
  );

  const registerDependenciesCode = `export function registerDependencies(app) { 
    const references = [${files.map((file) => `path + "${file}"`).join(",")}]; 
    app.defineBlazorReferences(references);
  }`;

  //Options
  const registerOptionsCode = `export function registerOptions(app) {
    app.defineBlazorOptions({ resourcePathRoot: path });
  }`;

  // Setup file
  const setupFilePath = join(piralconfigfolder, setupfile).replace(/\\/g, "/");
  const setupFileExists = existsSync(setupFilePath);

  if (setupFileExists) {
    allImports.push(`import projectSetup from '${setupFilePath}';`);
  }

  const setupPiletCode = `export function setupPilet(api) {
    ${cssLinks.map(href => `withCss(${JSON.stringify(href)});`).join('\n')}
    ${setupFileExists ? "projectSetup(api);" : ""}
  }`;

  // Teardown file
  const teardownFilePath = join(piralconfigfolder, teardownfile).replace(
    /\\/g,
    "/"
  );
  const teardownFileExists = existsSync(teardownFilePath);

  if (teardownFileExists) {
    allImports.push(`import projectTeardown from '${teardownFilePath}';`);
  }

  const teardownPiletCode = `export function teardownPilet(api) {
    ${cssLinks.map(href => `withoutCss(${JSON.stringify(href)});`).join('\n')}
    ${teardownFileExists ? "projectTeardown(api);" : ""}
  }`;

  const headCode = makePiletHead(
    allImports,
    getPiralBlazorApiCode,
    setupPiletCode,
    teardownPiletCode,
    registerDependenciesCode,
    registerOptionsCode
  );

  try {
    const { routes, extensions } = await analyzeProject(blazorprojectfolder);
    const standardPiletCode = makePiletCode(routes, extensions);

    return `
      ${headCode}

      ${standardPiletCode}
    `;
  } catch (err) {
    console.error(err);

    return `
      ${headCode}    

      ${fallbackPiletCode}
    `;
  }
};
