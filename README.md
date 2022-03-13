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
        // configure dependency injection here
    }
}
```

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

## License

Piral.Blazor is released using the MIT license. For more information see the [license file](./LICENSE).
