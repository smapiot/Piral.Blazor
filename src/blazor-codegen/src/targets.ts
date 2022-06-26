import { basename } from "path";
import { targetFramework, targetFrameworkAlt } from "./constants";
import { ProjectAssets, Targets } from "./types";

function createAllRefs(
  internaltargets: Record<string, Array<string>>,
  externalTargets: Record<string, Array<string>>
) {
  //Sets de-duplicate AND keep their insertion order
  const refs = new Set<string>();

  const createExternalRefs = (fileName: string) => {
    //depth-first post-order traversal of the dependencies
    if (!fileName) return;
    const deps = externalTargets[fileName];
    if (!deps || deps.length === 0) return refs.add(fileName);
    deps.forEach(createExternalRefs);
    refs.add(fileName);
  };

  for (const [fileName, deps] of Object.entries(internaltargets)) {
    deps.forEach(createExternalRefs);
    refs.add(fileName);
  }

  return [...refs];
}

function defineTargets(
  uniqueDependencies: Array<string>,
  projectAssets: ProjectAssets
): [internal: Targets, external: Targets] {
  const isNotSharedDep = (x: string | undefined) =>
    typeof x === "string" && uniqueDependencies.includes(x);
  const stripVersion = (x: string) => x.split("/")[0];

  // Get all external dependencies
  const targets =
    projectAssets.targets?.[targetFrameworkAlt] ??
    projectAssets.targets?.[targetFramework];

  /**Looks up the dll name for a project id */
  const getDllName = (projectId: string) => {
    const target = Object.entries(targets).find(
      (t) => stripVersion(t[0]) === projectId
    );

    const compile = target?.[1]?.compile;

    if (!compile) {
      return undefined;
    }

    return ((Object.keys(compile)?.[0] ?? "").split("/").pop() ?? "").replace(
      ".dll",
      ""
    );
  };

  const getcsprojname = (x: string) => basename(x).slice(0, -7); // C:\\path\\to\\proj\\proj.csproj --> proj

  const filterDeps = (deps: Array<string>) =>
    deps
      .map(getDllName)
      .filter((d) => !!d && isNotSharedDep(d)) as Array<string>;

  const externalTargets = Object.entries(targets)
    .map(([id, data]) => [getDllName(stripVersion(id)), data] as const)
    //filter out targets that are shared deps
    .filter(([dllName, _]) => isNotSharedDep(dllName))
    //filter out dependencies that are shared deps
    .map(
      ([dllName, data]) =>
        [
          dllName as string,
          filterDeps(Object.keys(data.dependencies || {})),
        ] as const
    )
    // key-value to object
    .reduce((acc, [k, v]) => ({ [k]: v, ...acc }), {});

  // Get internal project
  const projectName = projectAssets.project?.restore?.projectName;

  const projectDependencies = filterDeps(
    Object.keys(
      projectAssets.project?.frameworks?.[targetFramework]?.dependencies ?? {}
    )
  );

  const projectReferences = Object.keys(
    projectAssets.project?.restore?.frameworks?.[targetFramework]
      ?.projectReferences ?? {}
  ).map(getcsprojname);

  const internalTargets = {
    // depencency arr = deps + references
    [projectName]: [...projectDependencies, ...projectReferences],
  };

  return [internalTargets, externalTargets];
}

export function createAllTargetRefs(
  uniqueDependencies: Array<string>,
  projectAssets: ProjectAssets
) {
  const targets = defineTargets(uniqueDependencies, projectAssets);
  return createAllRefs(...targets);
}
