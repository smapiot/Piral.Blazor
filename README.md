[![Piral Logo](https://github.com/smapiot/piral/raw/master/docs/assets/logo.png)](https://piral.io)

# Piral.Blazor &middot; [![GitHub License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/smapiot/piral.blazor/blob/master/LICENSE) [![Build Status](https://smapiot.visualstudio.com/piral-pipelines/_apis/build/status/smapiot.piral.blazor?branchName=blazor-5.0)](https://smapiot.visualstudio.com/piral-pipelines/_build/latest?definitionId=48&branchName=blazor-5.0) [![GitHub Tag](https://img.shields.io/github/tag/smapiot/Piral.Blazor.svg)](https://github.com/smapiot/Piral.Blazor/releases) [![GitHub Issues](https://img.shields.io/github/issues/smapiot/piral.svg)](https://github.com/smapiot/piral/issues) [![Gitter Chat](https://badges.gitter.im/gitterHQ/gitter.png)](https://gitter.im/piral-io/community) [![Feed Status](https://img.shields.io/uptimerobot/status/m783654792-cfe3913c7481e0f44c143f63)](https://status.piral.io/)

All .NET things to make <a href="https://blazor.net" rel="nofollow"><img
src="https://devblogs.microsoft.com/aspnet/wp-content/uploads/sites/16/2019/04/BrandBlazor_nohalo_1000x.png"
height="10">&nbsp;Blazor</a> work seamlessly in microfrontends using
<a href="https://piral.io" rel="nofollow">
<img src="https://piral.io/logo-simple.f8667084.png" height="10">
&nbsp;Piral</a>.

## Getting Started

> You'll also find some information in the [piral-blazor](https://www.npmjs.com/package/piral-blazor) package.

To create a Blazor pilet using Piral.Blazor, two approaches can be used:

#### 1. Creating a Blazor pilet from scratch.

In this case, it is highly recommended to use our template. More information and installation instructions can be found in [`Piral.Blazor.Template`](/src/Piral.Blazor.Template)

#### 2. Transforming an existing Blazor application into a pilet

In this case, follow these steps:

1. Add a `PiralInstance` property to your `.csproj` file (The Piral instance name should be the name of the Piral instance you want to use, as it is published on npm.)

   ```xml
   <PropertyGroup>
       <TargetFramework>net5.0</TargetFramework>
       <PiralInstance>my-piral-instance</PiralInstance>
   </PropertyGroup>
   ```

2. Install the `Piral.Blazor.Tools` and `Piral.Blazor.Utils` packages
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

### Components, tiles, menu items...

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

### Dependency injection

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

## Running and debugging the pilet :rocket:

From your blazor project folder, run your pilet via the Piral CLI:

```sh
npx pilet debug --base ../piral~/<project-name>
```

In addition to this, if you want to debug your Blazor pilet using for example Visual Studio, these requirements should be considered:

- keep the Piral CLI running
- debug your Blazor pilet using IISExpress

## License

Piral.Blazor is released using the MIT license. For more information see the [license file](./LICENSE).
