import * as React from "react";
// @ts-ignore
import type { PiletApi } from "../piral~/Pilet/node_modules/app-shell";

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
        <div style={{ padding: '1em', background: '#efefef', flex: 1 }}>
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
      initialColumns: 4,
      initialRows: 8,
    }
  );

  app.registerExtension("try-order", () => <div>3</div>, { order: 3 });

  app.registerExtension("try-order", () => <div>1</div>, { order: 1 });

  app.registerExtension("try-order", () => <div>2</div>, { order: 2 });

  app.registerExtension("try-order", () => <div>0</div>, { order: 0 });

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
  app.registerMenu(app.fromBlazor("other-menu"));
};
