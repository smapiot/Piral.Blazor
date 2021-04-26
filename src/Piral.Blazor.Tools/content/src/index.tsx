import { PiletApi } from '**PiralInstance**';
import * as Blazor from './blazor.codegen';

export function setup(app: PiletApi) {
    Blazor.registerDependencies(app);
    Blazor.registerOptions(app);
    Blazor.registerPages(app);
    Blazor.registerExtensions(app);
    Blazor.setupPilet(app);
}
