import * as React from 'react';
import { PiletApi } from '**PiralInstance**';
import { registerDependencies, registerBlazorPages, registerBlazorExtensions } from './blazor.codegen';

export function setup(app: PiletApi) {
    registerDependencies(app);
    registerBlazorPages(app);
    registerBlazorExtensions(app);
    
    //TODO
}