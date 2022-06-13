import { PiletApi } from '**PiralInstance**';
import * as Blazor from './blazor.codegen';
import './**BlazorProjectName**.styles.css';

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
    app.releaseBlazorReferences?.();
}
