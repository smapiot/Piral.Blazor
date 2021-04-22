import * as React from 'react';
import { PiletApi } from '**PiralInstance**';
import {
    registerDependencies,
    registerBlazorPages,
    registerBlazorExtensions,
    registerBlazorOptions,
    setupPilet,
} from './blazor.codegen';

export function setup(app: PiletApi) {
    registerDependencies(app);
    registerBlazorOptions(app);
    registerBlazorPages(app);
    registerBlazorExtensions(app);
    setupPilet(app);
}
