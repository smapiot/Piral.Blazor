import * as React from "react";
import type { PiletApi } from "../piral~/Pilet/node_modules/sample-piral";

export default (app: PiletApi) => {
  app.registerExtension("react-counter", ({ params }) => {
    const inc = params.diff || 1;
    const [count, setCount] = React.useState(params.count || 0);
    const increment = React.useCallback(() => setCount((c) => c + inc), []);
    return (
      <div>
        <button onClick={increment}>{count}</button>
      </div>
    );
  });

  app.registerMenu(app.fromBlazor("counter-menu"));
};
