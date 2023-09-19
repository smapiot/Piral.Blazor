import * as React from "react";
import { render } from "react-dom";
import { Piral, createInstance, createStandardApi } from "piral";
import { createBlazorApi } from "piral-blazor";
import { layout, errors } from "./layout";

// change to your feed URL here (either using feed.piral.cloud or your own service)
const feedUrl = "https://feed.piral.cloud/api/v1/pilet/blazor-demo";

const instance = createInstance({
  state: {
    components: layout,
    errorComponents: errors,
  },
  plugins: [...createStandardApi(), createBlazorApi()],
  requestPilets() {
    return fetch(feedUrl)
      .then((res) => res.json())
      .then((res) => res.items);
  },
});

render(<Piral instance={instance} />, document.querySelector("#app"));
