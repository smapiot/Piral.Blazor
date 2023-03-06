import { promisify } from "util";
import { exec, spawn } from "child_process";
import { getPiralVersion } from "./piral";
import { action, analyzer, configuration } from "./constants";
import { ProjectConfig } from "./types";

const execAsync = promisify(exec);

export async function buildSolution(cwd: string) {
  console.log(`Running "${action}" on solution in ${configuration} mode...`);

  process.env.PIRAL_BLAZOR_RUNNING = "yes";

  return new Promise<void>((resolve, reject) => {
    const ps = spawn(`dotnet`, [action, "--configuration", configuration], {
      cwd,
      env: process.env,
      detached: false,
      stdio: "inherit",
    });

    ps.on("error", reject);
    ps.on("exit", resolve);
  });
}

export async function checkInstallation(
  blazorVersion: string,
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
    const installCmd = `npm i blazor@${blazorVersion} piral-blazor@${piralVersion} --no-save --legacy-peer-deps`;
    await execAsync(installCmd);
  }
}

export async function analyzeProject(
  config: ProjectConfig
) {
  const command = `dotnet ${analyzer} --base-dir "${config.projectDir}" --dll-name "${config.projectName}.dll" --target-framework "${config.targetFramework}" --configuration "${configuration}"`;
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
