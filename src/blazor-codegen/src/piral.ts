import { existsSync } from "fs";
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
  const appdir = resolve(baseFolder, "node_modules", piralName);

  if (!existsSync(`${appdir}/app`) && existsSync(`${appdir}/dist`)) {
    return `${appdir}/dist`;
  }

  return appdir;
}
