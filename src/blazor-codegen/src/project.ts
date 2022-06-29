import glob from "glob";
import { basename } from "path";
import { promisify } from "util";
import { exec, spawn } from "child_process";
import { getPiralVersion } from "./piral";
import { action, analyzer, configuration, targetFramework } from "./constants";

const execAsync = promisify(exec);
const spawnAsync = promisify(spawn);

const matchVersion = /\d+\.\d+\.\d+/;

/** Extracts the project name from a blazor project folder */
export function getProjectName(projectFolder: string) {
  return new Promise((resolve, reject) => {
    glob(`${projectFolder}/*.csproj`, (err, matches) => {
      if (!!err || !matches || matches.length == 0)
        return reject(new Error(`Project file not found. Details: ${err}`));
      if (matches.length > 1)
        return reject(
          new Error(
            `Only one project file is allowed. You have: ${JSON.stringify(
              matches,
              null,
              2
            )}`
          )
        );
      return resolve(basename(matches[0]).replace(".csproj", ""));
    });
  });
}

export async function buildSolution(cwd: string) {
  console.log(`Running "${action}" on solution in ${configuration} mode...`);

  process.env.PIRAL_BLAZOR_RUNNING = 'yes';

  await spawnAsync(`dotnet`, [action, '--configuration', configuration], {
    cwd,
    env: process.env,
    stdio: 'inherit',
  });
}

export async function checkInstallation(
  piletBlazorVersion: string,
  shellPackagePath: string
) {
  try {
    require.resolve("piral-blazor/package.json");
    require.resolve("blazor/package.json");
  } catch {
    console.warn(
      "The npm packages `blazor` and `piral-blazor` have not been not found. Installing them now..."
    );
    const piralVersion = getPiralVersion(shellPackagePath);
    const result = matchVersion.exec(piletBlazorVersion);

    if (!result) {
      throw new Error(
        "Could not detect version of Blazor. Something does not seem right."
      );
    }

    const [npmBlazorVersion] = result;
    const [blazorRelease] = npmBlazorVersion.split(".");
    const installCmd = `npm i blazor@^${blazorRelease} piral-blazor@${piralVersion}`;
    await execAsync(installCmd);
  }
}

export async function analyzeProject(blazorprojectfolder: string) {
  const projectName = await getProjectName(blazorprojectfolder);
  const command = `dotnet ${analyzer} --base-dir "${blazorprojectfolder}" --dll-name "${projectName}.dll" --target-framework "${targetFramework}" --configuration "${configuration}"`;
  const { stdout, stderr } = await execAsync(command);

  if (stderr) {
    throw new Error(stderr);
  }

  const { routes, extensions } = JSON.parse(stdout.trim()) as {
    routes: Array<string>;
    extensions: Record<string, Array<string>>;
  };
  return { routes, extensions };
}
