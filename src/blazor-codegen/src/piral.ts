import { resolve } from "path";

export function getPiralVersion(shellPackagePath: string) {
  try {
    const { version } = require(shellPackagePath).piralCLI;

    if (typeof version !== "string") {
      throw new Error();
    }

    return version;
  } catch {
    try {
      return require("piral-cli/package.json").version;
    } catch {
      throw new Error(
        "The version of the `piral-cli` could not be determined."
      );
    }
  }
}

export function findAppDir(baseFolder: string, piralName: string) {
  try {
    const path = require.resolve(`${piralName}/package.json`, {
      paths: [baseFolder],
    });
    return resolve(path, "..");
  } catch {
    return resolve(baseFolder, "node_modules", piralName);
  }
}
