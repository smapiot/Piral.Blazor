/** Generates the source code line to register a blazor page */
export function toPageRegistration(route: string) {
  return `app.registerPage("${toPath(
    route
  )}", app.fromBlazor("page-${route}"));`;
}

/** Generates the source code line to register a blazor extension */
export function toExtensionRegistration(entry: [string, Array<string>]) {
  const fqn = entry[0];
  const ids = entry[1];
  return ids
    .map(
      (id) =>
        `app.registerExtension("${id}", app.fromBlazor("extension-${fqn}"));`
    )
    .join("\n");
}

/** Translate a Blazor route into path-to-regexp syntax */
export function toPath(route: string) {
  return route.replace(/\{([\w?]*)([:*])?([^\/\{\}]*)\}/g, (...groups) =>
    groups[2] != "*" ? `:${groups[1]}` : "*"
  );
}
