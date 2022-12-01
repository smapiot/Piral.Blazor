import { join, resolve } from "path";
import { existsSync } from "fs";
import { getAssetPath, getFilePath } from "./io";
import { rebuildNeeded } from "./utils";
import { createAllTargetRefs } from "./targets";
import { prepare } from "./prepare";
import { analyzeProject, buildSolution } from "./project";
import { ProjectAssets, StaticAssets } from "./types";
import {
  fallbackPiletCode,
  makePiletCode,
  makePiletHead,
  standaloneRemapCode,
} from "./snippets";
import {
  configuration,
  targetFramework,
  setupfile,
  blazorprojectfolder,
  isRelease,
  pajson,
  swajson,
  configFolderName,
  teardownfile,
} from "./constants";

const bv = "PIRAL_BLAZOR_LAST_BUILD";
const piralconfigfolder = resolve(blazorprojectfolder, configFolderName);
const objectsDir = resolve(blazorprojectfolder, "obj");
const pafile = resolve(objectsDir, pajson);
const swafile = resolve(objectsDir, configuration, targetFramework, swajson);

module.exports = async function () {
  const allImports: Array<string> = [];
  const targetDir = this.options.outDir;

  // always build when files not found or in release
  // never re-build just when there is a change incoming
  if (!process.env[bv] && (isRelease || rebuildNeeded(pafile, swafile))) {
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

  const { standalone, manifest, dlls, pdbs } = await prepare(
    targetDir,
    staticAssets
  );

  this.addDependency(swafile);
  this.addDependency(pafile);
  this.addDependency(manifest);

  if (standalone) {
    // Integrate API usually provided by piral-blazor
    allImports.push(
      `import * as pbc from 'piral-blazor/convert';`
    );
  }

  const getPiralBlazorApiCode = `export function initPiralBlazorApi(app) {
    ${standalone ? standaloneRemapCode : ''}
  }`;

  // Refs
  const uniqueDependencies = dlls.map((f) => f.replace(".dll", ""));

  // Find out if there are ApplicationBundle files, otherwise take ProjectBundle files
  const traitValue =
    staticAssets.Assets.find((m) => m.AssetTraitValue === "ApplicationBundle")
      ?.AssetTraitValue ?? "ProjectBundle";
  const bundleFiles = staticAssets.Assets.filter(
    (m) => m.AssetTraitValue === traitValue
  );

  // Get the CSS files for the project
  const cssLinks = bundleFiles
    .filter((m) => m.AssetTraitName === "ScopedCss")
    .map(getAssetPath);

  // Dervice files
  const refs = createAllTargetRefs(uniqueDependencies, projectAssets);
  const files = [...refs.map((ref) => `${ref}.dll`), ...pdbs].map((name) =>
    getFilePath(staticAssets, name)
  );

  const registerDependenciesCode = `export function registerDependencies(app) {
    const references = ${JSON.stringify(files)}.map((file) => path + file);
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
    const promises = [];
    const addScript = (href) => {
      promises.push(new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = path + href;
        script.onerror = () => reject(new Error('Loading the script failed:' + href));
        script.onload = () => resolve();
        document.body.appendChild(script);
      }));
    };
    ${cssLinks.map((href) => `withCss(${JSON.stringify(href)});`).join("\n")}
    ${setupFileExists ? "projectSetup(api, addScript);" : ""}
    return Promise.all(promises);
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
    ${cssLinks.map((href) => `withoutCss(${JSON.stringify(href)});`).join("\n")}
    ${teardownFileExists ? "projectTeardown(api);" : ""}

    if (typeof api.releaseBlazorReferences === 'function') {
      api.releaseBlazorReferences();
    }
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

    process.env[bv] = `time:${Date.now()}`;

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
