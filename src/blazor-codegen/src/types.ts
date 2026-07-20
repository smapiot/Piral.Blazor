export interface BlazorManifest {
  cacheBootResources: boolean;
  config: Array<string>;
  debugBuild: boolean;
  entryAssembly: string;
  icuDataMode: number;
  linkerEnabled: boolean;
  resources: {
    hash?: string;
    // maps fingerprinted filenames to non-fingerprinted ones (e.g., "CommunityToolkit.Mvvm.ek2f7r3onp.wasm": "CommunityToolkit.Mvvm.wasm")
    fingerprinting?: Record<string, string>;
    assembly: Record<string, string>;
    coreAssembly?: Record<string, string>;
    pdb: Record<string, string>;
    runtime?: Record<string, string>;
    jsModuleNative?: Record<string, string>;
    jsModuleRuntime?: Record<string, string>;
    wasmNative?: Record<string, string>;
    icu?: Record<string, string>;
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
  kind: string;
  sharedDependencies: Array<string>;
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
          {
            suppressParent?: string;
            autoReferenced?: boolean;
            target: string;
            version: string;
          }
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
  Fingerprint?: string;
  AssetKind: string;
  AssetMode: string;
  AssetRole: string;
  RelatedAsset: string;
  AssetTraitName: string;
  AssetTraitValue: string;
  Integrity: string;
  CopyToOutputDirectory: string;
  CopyToPublishDirectory: string;
  OriginalItemSpec: string;
}

export interface DerivedAssets {
  /**
   * The referenced / used assemblies
   */
  assemblies: Array<{
    /**
     * Id of the assembly, e.g., "MyPilet.wasm"
     */
    id: string;
    /**
     * Name of the file to copy, e.g., MyPilet.abcdef1234.wasm
     */
    name: string;
    /**
     * Full source path of the file, e.g., /home/foo/bar/etc/bin/MyPilet.abcdef1234.wasm
     */
    source: string;
    /**
     * Full target path of the file, e.g., /home/foo/bar/~piral/dist/_framework/MyPilet.abcdef1234.wasm
     */
    target: string;
    /**
     * Fingerprint used by the file, e.g., .abcdef1234 - empty to denote no fingerprint
     */
    fingerprint: string;
    /**
     * True indicates that this is just a dependency of the main project, otherwise false
     */
    dependency: boolean;
    /**
     * True indicates that this is the main entry point of the pilet, otherwise false
     */
    entry: boolean;
    /**
     * True indicates that the assembly is already present (in one form or another) in the parent and should be ignored
     */
    ignored: boolean;
  }>;
  /**
   * The debug symbols for the assembly, if any
   */
  symbols: Array<{
    /**
     * Id of the symbols file, e.g., "MyPilet.pdb"
     */
    id: string;
    /**
     * Name of the file to copy, e.g., MyPilet.abcdef1234.pdb
     */
    name: string;
    /**
     * Full source path of the file, e.g., /home/foo/bar/etc/bin/MyPilet.abcdef1234.pdb
     */
    source: string;
    /**
     * Full target path of the file, e.g., /home/foo/bar/~piral/dist/_framework/MyPilet.abcdef1234.pdb
     */
    target: string;
    /**
     * Fingerprint used by the file, e.g., .abcdef1234 - empty to denote no fingerprint
     */
    fingerprint: string;
    /**
     * True indicates that this is the main entry point of the pilet, otherwise false
     */
    entry: boolean;
    /**
     * True indicates that the symbols file is already present (in one form or another) in the parent and should be ignored
     */
    ignored: boolean;
  }>;
  /**
   * The static web asset files which are not assemblies
   */
  files: Array<{
    /**
     * Id of the file, e.g., MyPilet.css
     */
    id: string;
    /**
     * Name of the file, e.g., MyPilet.52553.css
     */
    name: string;
    /**
     * The type of asset, e.g., "css" is a stylesheet
     */
    type: "css" | "other";
    /**
     * Fingerprint used by the file, e.g., .abcdef1234 - empty to denote no fingerprint
     */
    fingerprint: string;
    /**
     * Full source path of the file, e.g., /home/foo/bar/etc/bin/MyPilet.52553.css
     */
    source: string;
    /**
     * Full target path of the file, e.g., /home/foo/bar/~piral/dist/_framework/MyPilet.52553.css
     */
    target: string;
  }>;
  /**
   * The generated satellite assemblies / files
   */
  satellites: Array<{
    /**
     * Id of the assembly, e.g., "MyPilet.resources.wasm"
     */
    id: string;
    /**
     * The culture of the satellite, e.g., "de".
     */
    culture: string;
    /**
     * Name of the file to copy, e.g., MyPilet.resources.abcdef1234.wasm
     */
    name: string;
    /**
     * Full source path of the file, e.g., /home/foo/bar/etc/bin/MyPilet.resources.abcdef1234.wasm
     */
    source: string;
    /**
     * Full target path of the file, e.g., /home/foo/bar/~piral/dist/_framework/MyPilet.resources.abcdef1234.wasm
     */
    target: string;
    /**
     * Fingerprint used by the file, e.g., .abcdef1234 - empty to denote no fingerprint
     */
    fingerprint: string;
  }>;
}
