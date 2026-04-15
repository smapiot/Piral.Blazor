import { join } from "path";

import { checkExists } from "./io";
import { rebuildNeeded } from "./utils";
import { prepare } from "./prepare";
import { analyzeProject, buildSolution } from "./project";
import { getProjectConfig } from "./config";
import {
  fallbackPiletCode,
  makePiletCode,
  makePiletHead,
  standaloneRemapCode,
} from "./snippets";
import {
  setupfile,
  blazorprojectfolder,
  isRelease,
  teardownfile,
} from "./constants";

const bv = "PIRAL_BLAZOR_LAST_BUILD";

module.exports = async function () {
  const allImports: Array<string> = [];
  // @ts-ignore
  const targetDir = this.options.outDir;
  const config = await getProjectConfig(blazorprojectfolder);

  // always build when files not found or in release
  // never re-build just when there is a change incoming
  if (!process.env[bv] && (isRelease || (await rebuildNeeded(config)))) {
    try {
      await buildSolution(blazorprojectfolder);
    } catch (err) {
      throw new Error(
        [
          `Something went wrong with the Blazor build.`,
          `Make sure there is at least one Blazor project in your solution.`,
          `Seen error: ${err}`,
        ].join("\n"),
      );
    }
  }

  const { standalone, assets, watchlist } = await prepare(targetDir, config);

  // @ts-ignore
  watchlist.forEach((path) => this.addDependency(path));

  if (standalone) {
    // Integrate API usually provided by piral-blazor
    allImports.push(`import * as pbc from 'piral-blazor/convert';`);
  }

  const getPiralBlazorApiCode = `export function initPiralBlazorApi(app) {
    const handler = async (ev) => {
      const { responseTo, fn, args } = ev;
      let result = null;

      if (typeof app[fn] === 'function') {
        result = await app[fn].call(app, ...args);
      } else if (typeof fn === 'string' && fn.indexOf('.') > 0) {
        const parts = fn.split('.');
        const name = parts.pop();
        let ctx = app;

        parts.forEach(part => {
          if (ctx && part in ctx) {
            ctx = ctx[part];
          }
        });

        if (ctx && typeof ctx[name] === 'function') {
          ctx[name].call(ctx, ...args);
        }
      }

      app.emit(responseTo, result);
    };
    const interopEvent = \`blazor-interop-\${app.meta.name}@\${app.meta.version}\`;
    app.on(interopEvent, handler);
    app.unwire = () => app.off(interopEvent, handler);
    ${standalone ? standaloneRemapCode : ""}
  }`;

  // Get the CSS files for the project

  // Derive files
  const references = assets.assemblies
    .filter((m) => !m.ignored)
    .flatMap((m) => [m.name, m.symbols?.name].filter(Boolean));

  const cssLinks = assets.files
    .filter((m) => m.type === "css")
    .map((m) => m.name);

  const satellites = Object.fromEntries(
    Object.entries(assets.satellites).map(([name, files]) => [
      name,
      files.map((m) => m.name),
    ]),
  );

  const registerDependenciesCode = `export function registerDependencies(app) {
    const references = ${JSON.stringify(references)}.map((file) => path + file);
    const satellites = ${JSON.stringify(satellites) || "undefined"};
    app.defineBlazorReferences(references, satellites, ${
      config.priority
    }, ${JSON.stringify(config.kind)}, ${JSON.stringify(
      config.sharedDependencies,
    )});
  }`;

  //Options
  const registerOptionsCode = `export function registerOptions(app) {
    app.defineBlazorOptions({ resourcePathRoot: path });
  }`;

  // Setup file
  const setupFilePath = join(config.configDir, setupfile).replace(/\\/g, "/");
  const setupFileExists = await checkExists(setupFilePath);

  if (setupFileExists) {
    allImports.push(`import projectSetup from '${setupFilePath}';`);
  }

  const setupPiletCode = `export function setupPilet(api) {
    const promises = [];
    const addScript = (href, attrs = {}) => {
      promises.push(new Promise((resolve, reject) => {
        const src = path + href;
        const exists = document.querySelector('script[src="' + src + '"]');

        if (!exists) {
          const script = document.createElement('script');
          Object.entries(attrs).forEach(([name, value]) => script.setAttribute(name, value));
          script.src = src;
          script.onerror = () => reject(new Error('Loading the script failed:' + href));
          script.onload = () => resolve();
          document.body.appendChild(script);
        }
      }));
    };
    ${cssLinks.map((href) => `addStyle(${JSON.stringify(href)});`).join("\n")}
    ${setupFileExists ? "projectSetup(api, addScript, addStyle);" : ""}
    return Promise.all(promises);
  }`;

  // Teardown file
  const teardownFilePath = join(config.configDir, teardownfile).replace(
    /\\/g,
    "/",
  );
  const teardownFileExists = await checkExists(teardownFilePath);

  if (teardownFileExists) {
    allImports.push(`import projectTeardown from '${teardownFilePath}';`);
  }

  const teardownPiletCode = `export function teardownPilet(api) {
    api.unwire();
    removeStyles();
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
    registerOptionsCode,
  );

  try {
    const { routes, extensions } = await analyzeProject(config);
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
