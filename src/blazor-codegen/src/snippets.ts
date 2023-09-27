import {
  toExtensionRegistration,
  toPageRegistration,
  toPath,
} from "./components";

export const definePathCode = `function computePath() {
  try {
    throw new Error();
  } catch (t) {
    const e = ('' + t.stack).match(/(https?|file|ftp|chrome-extension|moz-extension):\\/\\/[^)\\n]+/g);
    if (e) {
      return e[0].replace(/^((?:https?|file|ftp|chrome-extension|moz-extension):\\/\\/.+)\\/[^\\/]+$/, '$1') + '/';
    }
  }
  return '/';
}
const path = computePath();
`;

export const handleCssCode = `const styles = [];

function addStyle(href, pos) {
  const exists = document.querySelector('link[data-src="' + href + '"]');

  if (!exists) {
    const link = document.createElement('link');
    link.dataset.src = href;
    link.rel = 'stylesheet';
    link.href = path + href;
    styles.push(link);

    switch (pos) {
      case 'first':
        document.head.insertBefore(link, document.head.firstChild);
        break;
      case 'before':
        document.head.insertBefore(link, styles[0]);
        break;
      case 'after':
      case 'last':
      default:
        document.head.appendChild(link);
        break;
    }
  }
}

function removeStyles() {
  for (let i = styles.length; i--; ) {
    styles[i].remove();
    styles.splice(i, 1);
  }
}`;

export const standaloneRemapCode = `
  app.defineBlazorReferences = (refs, satellites, prio) => pbc.defineBlazorReferences(refs, app.meta, satellites, prio);
  app.defineBlazorOptions = pbc.defineBlazorOptions || (() => {});
  app.fromBlazor = pbc.fromBlazor;
  app.releaseBlazorReferences = pbc.releaseBlazorReferences;
`;

export const fallbackPiletCode = `export function registerPages(...args) {
  console.warn('${__filename}: \`registerPages\` was called, but no Blazor routes were found.');
}

export function registerExtensions(...args) {
  console.warn('${__filename}: \`registerExtensions\` was called, but no Blazor extensions were found.');
}

export const routes = [];

export const paths = [];`;

export function makePiletHead(
  allImports: Array<string>,
  getPiralBlazorApiCode: string,
  setupPiletCode: string,
  teardownPiletCode: string,
  registerDependenciesCode: string,
  registerOptionsCode: string
) {
  return `
    ${allImports.join("\n")}

    ${definePathCode}
    ${handleCssCode}
    
    ${getPiralBlazorApiCode}
    ${setupPiletCode}
    ${registerDependenciesCode}
    ${registerOptionsCode}
    ${teardownPiletCode}
  `;
}

export function makePiletCode(
  routes: Array<string>,
  extensions: Record<string, Array<string>>
) {
  return `
    export function registerPages(app) { 
      ${routes.map(toPageRegistration).join("\n")}
    }

    export function registerExtensions(app) {
      ${Object.entries(extensions).map(toExtensionRegistration).join("\n")} 
    }

    export const routes = ${JSON.stringify(routes)};

    export const paths = ${JSON.stringify(routes.map(toPath))};
  `;
}
