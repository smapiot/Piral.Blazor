import { PiletApi } from '$(piralInstance)';

export function setup(app: PiletApi) {
  app.defineBlazorReferences(require('./reference.codegen'));
  app.registerPage('/sample', app.fromBlazor('sample-page'));
}
