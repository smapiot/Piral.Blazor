import { PiletApi } from "app-shell";
import * as React from "react"; 

export function setup(app: PiletApi) {
  app.defineBlazorReferences(require("./refs.codegen")); 

  app.registerTile(
    () => (
      <div>
        Welcome to <b>Piral</b>!
      </div>
    ),
    {
      initialColumns: 2,
      initialRows: 2,
    }
  );

  app.registerMenu(app.fromBlazor("counter-menu"));
  app.registerPage("/counter", app.fromBlazor("counter"));
}
