import { resolve } from "path";

import { checkExists, loadJson } from "./io";

export async function getPiralVersion(shellPackagePath: string) {
  try {
    const shellData = await loadJson(shellPackagePath);
    const { version } = shellData.piralCLI;

    if (typeof version !== "string") {
      throw new Error();
    }

    return version;
  } catch {
    try {
      const path = require.resolve("piral-cli/package.json");
      const cliData = await loadJson(path);
      return cliData.version;
    } catch {
      throw new Error(
        "The version of the `piral-cli` could not be determined.",
      );
    }
  }
}

export async function findAppDir(baseFolder: string, piralName: string) {
  const appDir = resolve(baseFolder, "node_modules", piralName);
  const appDirExists = await checkExists(`${appDir}/app`);

  if (!appDirExists) {
    const appDistDir = `${appDir}/dist`;
    const appDistDirExists = await checkExists(appDistDir);

    if (appDistDirExists) {
      return appDistDir;
    }
  }

  return appDir;
}
