import { PiletApi } from "../piral~/Pilet/node_modules/app-shell";
import * as React from "react";

export default (app: PiletApi) => {
  app.registerTile(
    () => {
      const [data, setData] = React.useState(["a", "b", "c"]);

      React.useEffect(() => {
        const tid = setTimeout(() => {
          setData((data) => [...data, "d"]);
        }, 5000);

        return () => {
          clearTimeout(tid);
        };
      }, []);

      return (
        <div>
          Welcome to <b>Piral</b>!{" "}
          <app.Extension
            name="sample-extension"
            params={{
              Items: data,
              Test: "1st",
            }}
          />
          <app.Extension
            name="sample-extension"
            params={{
              Items: data,
              Test: "2nd",
            }}
          />
          <app.Extension
            name="sample-extension"
            params={{
              Items: data,
              Test: "3rd",
            }}
          />
        </div>
      );
    },
    {
      initialColumns: 2,
      initialRows: 2,
    }
  );

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
  app.registerPage("/counter", app.fromBlazor("counter-page"));
};
