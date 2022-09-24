import * as Blazor from './blazor.codegen';
import type { PiletApi } from '**MSBUILD_PiralInstance**';

export function setup(app: PiletApi) {
  Blazor.initPiralBlazorApi(app);
  Blazor.registerDependencies(app);
  Blazor.registerOptions(app);
  Blazor.registerPages(app);
  Blazor.registerExtensions(app);
  Blazor.setupPilet(app);
}

export function teardown(app: PiletApi) {
  Blazor.teardownPilet(app);
}
