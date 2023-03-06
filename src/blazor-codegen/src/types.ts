export interface BlazorManifest {
  cacheBootResources: boolean;
  config: Array<string>;
  debugBuild: boolean;
  entryAssembly: string;
  icuDataMode: number;
  linkerEnabled: boolean;
  resources: {
    assembly: Record<string, string>;
    pdb: Record<string, string>;
    runtime: Record<string, string>;
    extensions: any;
    lazyAssembly: any;
    libraryInitializers: any;
    satelliteResources: Record<string, Record<string, string>>;
  };
}

export interface ProjectConfig {
  targetFramework: string;
  configDir: string;
  paFile: string;
  swaFile: string;
  objectsDir: string;
  projectDir: string;
  projectName: string;
  priority: string;
}

export type BlazorResourceType = keyof BlazorManifest["resources"];

export type Targets = Record<string, Array<string>>;

export interface ProjectAssets {
  version: number;
  targets: Record<
    string,
    Record<
      string,
      {
        type: string;
        dependencies: Record<string, string>;
        runtime: Record<string, {}>;
        compile: Record<string, {}>;
      }
    >
  >;
  libraries: Record<
    string,
    {
      sha512: string;
      type: string;
      path: string;
      files: Array<string>;
    }
  >;
  projectFileDependencyGroups: Record<string, Array<string>>;
  packageFolders: Record<string, {}>;
  project: {
    version: string;
    runtimes: Record<string, Record<string, any>>;
    frameworks: Record<
      string,
      {
        targetAlias: string;
        dependencies: Record<
          string,
          Record<
            string,
            {
              suppressParent?: string;
              target: string;
              version: string;
            }
          >
        >;
        imports: Array<string>;
        warn: boolean;
        assetTargetFallback: boolean;
        downloadDependencies: Array<{ name: string; version: string }>;
        runtimeIdentifierGraphPath: string;
        frameworkReferences: Record<string, Record<string, string>>;
      }
    >;
    restore: {
      projectUniqueName: string;
      projectName: string;
      projectPath: string;
      packagesPath: string;
      outputPath: string;
      projectStyle: string;
      configFilePaths: Array<string>;
      originalTargetFrameworks: Array<string>;
      sources: Record<string, {}>;
      frameworks: Record<
        string,
        {
          targetAlias: string;
          projectReferences: Record<
            string,
            {
              projectPath: string;
            }
          >;
        }
      >;
      warningProperties: Record<string, Array<string>>;
    };
  };
}

export interface StaticAssets {
  Version: number;
  Hash: string;
  Source: string;
  BasePath: string;
  Mode: string;
  ManifestType: string;
  ReferencedProjectsConfiguration: Array<{
    Identity: string;
    Version: number;
    Source: string;
  }>;
  DiscoveryPatterns: Array<{
    Name: string;
    Source: string;
    ContentRoot: string;
    BasePath: string;
    Pattern: string;
  }>;
  Assets: Array<StaticAsset>;
}

export interface StaticAsset {
  Identity: string;
  SourceId: string;
  SourceType: string;
  ContentRoot: string;
  BasePath: string;
  RelativePath: string;
  AssetKind: string;
  AssetMode: string;
  AssetRole: string;
  RelatedAsset: string;
  AssetTraitName: string;
  AssetTraitValue: string;
  CopyToOutputDirectory: string;
  CopyToPublishDirectory: string;
  OriginalItemSpec: string;
}
