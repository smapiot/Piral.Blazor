# Piral.Blazor.Template

<!-- ! Version is "hardcoded" !  -->

[![Nuget](https://img.shields.io/badge/nuget-v3.2.1-blue)](https://www.nuget.org/packages/Piral.Blazor.Template/3.2.1)

For getting started easily, a Blazor template is available. This will set up a Blazor pilet for the preferred Piral instance.

### Installation

- Make sure that [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1) is installed.
- Run `dotnet new --install Piral.Blazor.Template::3.2.1` to install the project template globally.

### Usage

Create a new folder in your solution (the folder name will be used as your
project name) and run the template using

```sh
dotnet new blazorpilet --piralInstance <piral-instance-name>
```

The Piral instance name should be the name of the Piral instance as it is published on npm.

Build the project. The first time you do this, this can take some time as it
will scaffold the pilet.