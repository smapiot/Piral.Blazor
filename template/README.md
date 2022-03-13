[![Piral Logo](https://github.com/smapiot/piral/raw/main/docs/assets/logo.png)](https://piral.io)

# `blazor`

## What is it?

This package contains the shared dependencies for a Blazor micro frontend solution. This can be integrated into [Piral](https://piral.io) using the [`piral-blazor` converter](https://www.npmjs.com/package/piral-blazor/).

## What is inside?

The package contains the `Debug` and `Release` artifacts from building `Piral.Blazor.Core`, which is a project corresponding to a minimal Blazor application for rendering micro frontends.

Most importantly, the artifacts to be deployed with a web application are also contained in there. You'll get the `wwwroot/_framework` folder inside `debug` and `release`.

## How does `Piral.Blazor.Core` work?

`Piral.Blazor.Core` is a full Blazor application rendering to an element identified by the `#blazor-root` selector. In there, it will render every component that should currently be rendered inside its own `<div>`.

It comes with a set of functions ready for JS interop:

- `Task LoadComponentsFromLibrary(string url)`: Loads an assembly from the given URL and tries to register all components from it.
- `Task LoadComponentsWithSymbolsFromLibrary(string dllUrl, string pdbUrl)`: Loads an assembly with its associated debug helpers.
- `Task<string> Activate(string componentName, IDictionary<string, JsonElement> args)`: Activates a component and renders it with the given arguments.
- `Task Reactivate(string componentName, string referenceId, IDictionary<string, JsonElement> args)`: Refreshes the rendering of an active component by updating the arguments.
- `Task Deactivate(string componentName, string referenceId)`: Stops rendering a component.

A component renders individually and can be registered either implicitly or explicitly. For details on this [see the `Piral.Blazor` documentation](https://github.com/smapiot/Piral.Blazor).

## License

Piral.Blazor is released using the MIT license. For more information see the [license file](./LICENSE).
