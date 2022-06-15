[![Piral Logo](https://github.com/smapiot/piral/raw/main/docs/assets/logo.png)](https://piral.io)

# Piral.Blazor &middot; [![GitHub License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/smapiot/piral.blazor/blob/blazor-6.0/LICENSE) [![Build Status](https://smapiot.visualstudio.com/piral-pipelines/_apis/build/status/smapiot.piral.blazor?branchName=blazor-6.0)](https://smapiot.visualstudio.com/piral-pipelines/_build/latest?definitionId=48&branchName=blazor-6.0) [![GitHub Tag](https://img.shields.io/github/tag/smapiot/Piral.Blazor.svg)](https://github.com/smapiot/Piral.Blazor/releases) [![GitHub Issues](https://img.shields.io/github/issues/smapiot/piral.svg)](https://github.com/smapiot/piral/issues) [![Gitter Chat](https://badges.gitter.im/gitterHQ/gitter.png)](https://gitter.im/piral-io/community) [![Feed Status](https://img.shields.io/uptimerobot/status/m783654792-cfe3913c7481e0f44c143f63)](https://status.piral.io/)

All .NET things to make <a href="https://blazor.net" rel="nofollow"><img
src="https://devblogs.microsoft.com/aspnet/wp-content/uploads/sites/16/2019/04/BrandBlazor_nohalo_1000x.png"
height="10">&nbsp;Blazor</a> work seamlessly in microfrontends using
<a href="https://piral.io" rel="nofollow">
<img src="https://piral.io/logo-simple.f8667084.png" height="10">
&nbsp;Piral</a>.

> This is the branch for Blazor 6.0 with .NET 6.0. If you want to switch to Blazor with the older .NET Core 3.2, please refer to the [`blazor-3.2`](https://github.com/smapiot/Piral.Blazor/tree/blazor-3.2) or [`blazor-5.0`](https://github.com/smapiot/Piral.Blazor/tree/blazor-5.0) branch.

## Getting Started

> You'll also find some information in the [piral-blazor](https://www.npmjs.com/package/piral-blazor) package.

### Creating a Blazor Pilet

To create a Blazor pilet using `Piral.Blazor`, two approaches can be used:

#### 1. From Scratch

In this case, it is highly recommended to use our template. More information and installation instructions can be found in [`Piral.Blazor.Template`](/src/Piral.Blazor.Template)

[![Using Piral Blazor](https://img.youtube.com/vi/8kWkkNgE3ao/0.jpg)](https://www.youtube.com/watch?v=8kWkkNgE3ao)

#### 2. Transforming an Existing Application

In this case, follow these steps:

1. Add a `PiralInstance` property to your `.csproj` file (The Piral instance name should be the name of the Piral instance you want to use, as it is published on npm.)

   ```xml
   <PropertyGroup>
       <TargetFramework>net6.0</TargetFramework>
       <PiralInstance>my-piral-instance</PiralInstance>
   </PropertyGroup>
   ```

   (You can optionally also specify an `NpmRegistry` property. The default for this is set to `https://registry.npmjs.org/`)

2. Install the `Piral.Blazor.Tools` and `Piral.Blazor.Utils` packages, make sure they both have a version number of format `6.0.x`
3. rename `Program.cs` to `Module.cs`, and make sure to make the `Main` method an empty method.
4. Build the project. The first time you do this, this can take some time as it will fully scaffold the pilet.

## Usage

### Build Configuration

The `*.csproj` file of your pilet offers you some configuration steps to actually tailor the build to your needs.

Here is a minimal example configuration:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <PiralInstance>../../app-shell/dist/emulator/app-shell-1.0.0.tgz</PiralInstance>
  </PropertyGroup>

  <!-- ... -->
</Project>
```

This one gets the app shell from a local directory. Realistically, you'd have your app shell on a registry. In case of the default registry it could look like

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <PiralInstance>@mycompany/app-shell</PiralInstance>
  </PropertyGroup>

  <!-- ... -->
</Project>
```

but realistically you'd publish the app shell to a private registry on a different URL. In such scenarios you'd also make use of the `NpmRegistry` setting:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <PiralInstance>@mycompany/app-shell</PiralInstance>
    <NpmRegistry>https://registry.mycompany.com/</NpmRegistry>
  </PropertyGroup>

  <!-- ... -->
</Project>
```

Besides these two options (required `PiralInstance` and optional `NpmRegistry`) the following settings exist:

- `PiralInstance`: Sets the name (or local path) of the app shell.
- `NpmRegistry`: Sets the URL of the npm registry to use. Will be used for getting npm dependencies of the app shell (and the app shell itself).
- `Bundler`: Sets the name of the bundler to use. By default this is `webpack5`. The list of all available bundlers can be found [in the Piral documentation](https://docs.piral.io/reference/documentation/bundlers).
- `ProjectsWithStaticFiles`: Sets the names of the projects that contain static files, which require to be copied to the output directory. Separate the names of these projects by semicolons.
- `Monorepo`: Sets the behavior of the scaffolding to a monorepo mode. The value must be `enable` to switch this on.
- `PiralCliVersion`: Determines the version of the `piral-cli` tooling to use. By default this is `latest`.
- `Version`: Sets the version of the pilet. This is a/the standard project property.

A more extensive example:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Version>1.2.3</Version>
    <PiralInstance>@mycompany/app-shell</PiralInstance>
    <NpmRegistry>https://registry.mycompany.com/</NpmRegistry>
    <Bundler>esbuild</Bundler>
    <Monorepo>disable</Monorepo>
    <ProjectsWithStaticFiles>
      designsystem;
      someotherproject;
      thirdproj
    </ProjectsWithStaticFiles>
  </PropertyGroup>

  <!-- ... -->
</Project>
```

### Pages

A standard page in Blazor, using the `@page` directive, will work as expected, and will be automatically registered on the pilet API.

### Extensions

To register an extension, the `PiralExtension` attribute can be used. You will also have to provide the extension slot name that defines where the extension should be rendered. The component can even be registered into multiple slots using multiple attributes.

```razor
//counter.razor

@attribute [PiralExtension("my-counter-slot")]
@attribute [PiralExtension("another-extension-slot")]

<h1>Counter</h1>

<p>Current count: @currentCount</p>

<button @onclick="IncrementCount">Click me</button>

@code {
    int currentCount = 0;

    void IncrementCount()
    {
        currentCount++;
    }
}
```

To use an extension within a Blazor component, the `<Extension>` component can be used.

```razor
<Extension Name="my-counter-slot"></Extension>
```

### Components, Tiles, Menu Items, and Others

To register a Blazor component for use in the pilet API, the `PiralComponent` attribute can be used in two ways:

1. `[PiralComponent]`, this will register the component using the fully qualified name.
2. `[PiralComponent(<name>)]` will register the component using the custom name provided.

To register these components onto the pilet API, a `setup.tsx` file should be created at the root of your Blazor project.

This file may then, for example to register a tile, look like this:

```tsx
import { PiletApi } from '../piral~/<project_name>/node_modules/<piral_instance>';

export default (app: PiletApi) => {
	//for a component marked with[PiralComponent("my-tile")]
	app.registerTile(app.fromBlazor('my-tile'));
};
```

### Using Parameters

Parameters (or "props") are properly forwarded. Usually, it should be sufficient to declare `[Parameter]` properties in the Blazor components. Besides, there are more advanced ways.

For instance, to access the `params` prop of an extension you can use the `PiralParameter` attribute. This way, you can "forward" props from JS to the .NET name of your choice (in this case "params" is renamed to "Parameters").

```razor
@attribute [PiralExtension("sample-extension")]

<div>@Parameters.Test</div>

@code {
    public class MyParams
    {
        public string Test { get; set; }
    }

    [Parameter]
    [PiralParameter("params")]
    public MyParams Parameters { get; set; }
}
```

For the serialization you'll need to use either a `JsonElement` or something that can be serialized into. In this case, we used a class called `MyParams`.

With the `PiralParameter` you can also access / forward children to improve object access:

```razor
@attribute [PiralExtension("sample-extension")]

<div>@Message</div>

@code {
    [Parameter]
    [PiralParameter("params.Test")]
    public string Message { get; set; }
}
```

That way, we only have a property `Message` which reflects the `params.Test`. So if the extension is called like that:

```jsx
<app.Extension
    name="sample-extension"
    params={
        {
            Test: "Hello world",
        }
    }
/>
```

It would just work.

### Dependency Injection

You can define services for dependency injection in a `Module` class. The name of the class is arbitrary, but it shows the difference to the standard `Program` class, which should not be available, as mentioned before.

To be able to compile successfully, a `Main` method should be declared, which should remain empty.

```cs
public class Module
{
    public static void Main()
    {
        // this entrypoint should remain empty
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        // configure dependency injection for the components in the pilet here
        // -> use this for pilet-exclusive deps here
        // -> the method is optional; you can remove it if not needed
    }

    public static void ConfigureShared(IServiceCollection services)
    {
        // configure dependency injection for the whole application here
        // -> use this for third-party libraries or if you want to share deps with other pilets
        // -> the method is optional; you can remove it if not needed
    }
}
```

The `ConfigureServices` and `ConfigureShared` methods are optional. If you want to configure dependency injection in your pilet then use this. Our recommendation is to use `ConfigureServices` is much as possible, however, for using third-party libraries you should use `ConfigureShared`. Third-party libraries require globally shared dependencies, as the third-party libraries are also globally shared (i.e., if two pilets depend on the same assembly it would only be loaded once, making it implicitly shared).

## Running and Debugging the Pilet :rocket:

From your Blazor project folder, run your pilet via the Piral CLI:

```sh
cd ../piral~/<project-name>
npm start
```

In addition to this, if you want to debug your Blazor pilet using for example Visual Studio, these requirements should be considered:

- keep the Piral CLI running
- debug your Blazor pilet using IISExpress

> :warning: if you want to run your pilet and directly visit it in the browser without debugging via IISExpress, you will have to disable a [kras](https://github.com/FlorianRappl/kras) script injector **before** visiting the pilet. To do this, go to `http://localhost:1234/manage-mock-server/#/injectors`, disable the `debug.js` script, and save your changes. Afterwards, you can visit `http://localhost:1234`.

## Special Files

There are some special files that you can add in your project (adjacent to the *.csproj* file):

- *.piralconfig/setup.tsx*
- *.piralconfig/teardown.tsx*
- *.piralconfig/package-overwrites.json*
- *.piralconfig/js-imports.json*

Let's see what they do and how they can be used.

### Extending the Pilet's Setup

The *setup.tsx* file can be used to define more things that should be done in a pilet's `setup` function. By default, the content of the `setup` function is auto generated. Things such as `@page /path-to-use` components or components with `@attribute [PiralExtension("name-of-slot")]` would be automatically registered. However, already in case of `@attribute [PiralComponent]` we have a problem. What should this component do? Where is it used?

The solution is to use the *setup.tsx* file. An example:

```js
export default (app) => {
  app.registerMenu(app.fromBlazor('counter-menu'));

  app.registerExtension("ListToggle", app.fromBlazor('counter-preview'));
};
```

This example registers a pilet's component named "counter-menu" as a menu entry. Furthermore, it also adds the "counter-preview" component as an extension to the "ListToggle" slot.

Anything that is available on the Pilet API provided via the `app` argument is available in the function. The only import part of *setup.tsx* is that has a default export - which is actually a function.

### Overwriting the Package Manifest

The generated / used pilet is a standard npm package. Therefore, it will have a *package.json*. The content of this *package.json* is mostly pre-determined. Things such as `piral-cli` or the pilet's app shell package are in there. In some cases, additional JS dependencies for runtime or development aspects are necessary or useful. In such cases the *package-overwrites.json* comes in handy.

For instance, to actually extend the `devDependencies` you could write:

```json
{
  "devDependencies": {
    "axios": "^0.20.0"
  }
}
```

This would add a development dependency to the `axios` package. Likewise, other details, such as a publish config or a description could also be added / overwritten:

```json
{
  "description": "This is my pilet description.",
  "publishConfig": {
    "access": "public"
  }
}
```

The rules for the merge follow the [Json.NET](https://www.newtonsoft.com/json/help/html/MergeJson.htm) approach.

### Extending the Pilet's Teardown

The *teardown.tsx* file can be used to define more things that should be done in a pilet's `teardown` function. By default, the content of the `teardown` function is auto generated. Things such as `pages` and `extensions` would be automatically unregistered. However, in some cases you will need to unregister things manually. You can do this here.

### Defining Additional JavaScript Imports

Some Blazor dependencies require additional JavaScript packages in order to work correctly. The *js-imports.json* file can be to declare all these. The files will then be added via a generated `import` statement in the pilet's root module.

The content of the *js-imports.json* file is a JSON array. For example:

```json
[
  "axios",
  "global-date-functions"
]
```

Includes the two dependencies via the respective `import` statements.

## License

Piral.Blazor is released using the MIT license. For more information see the [license file](./LICENSE).
