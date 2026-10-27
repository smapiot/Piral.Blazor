# Patch Instructions

## Binary Patch

To create the modified `Microsoft.AspNetCore.Components.dll` you'll need to use Windows and the [dnSpy](https://github.com/dnSpyEx/dnSpy) tool.

1. Open the original `Microsoft.AspNetCore.Components.dll` located in the global NuGet folder (if there is none, make sure to first run `dotnet build`) using dnSpy
2. In the `Microsoft.AspNetCore.Components` namespace locate the `ComponentFactory` class
3. Open the IL editor ("Edit Method Body") of the `InstantiateComponent` method
4. Replace all instructions related to calling the `callvirt` of the `System.Action` with `nop` instructions (essentially everything after the `throw` in instruction 67 to instruction 77)
5. Run "Save Module" and either overwrite it in the location or directly replace the patch file here

So the original should look like this:

![Original Implementation](./instantiate-original.png)

And the modified version looks like this:

![Modified Implementation](./instantiate-modified.png)

## Source Patch

These steps can be automated.

1. `git clone https://github.com/dotnet/aspnetcore.git`
2. `cd aspnetcore` (to project root)
3. Change to the corresponding release tag (e.g., `v10.0.14` for .NET 10 release 14): `git checkout v10.0.14`
4. `git apply piral-blazor-source/tools/componentfactory-piral.patch` (don't forget to replace `piral-blazor-source` with the path to the actual `Piral.Blazor` repository)
5. `cd src/Components` (from project root)
6. `./build.sh` (on Linux, otherwise `build.cmd` on Windows)
7. Usually it would be good to also build for Release afterwards; `cd Components/src` (from `src/Components`) and `dotnet build Microsoft.AspNetCore.Components.csproj -c Release`
8. Now use / copy `artifacts/bin/Microsoft.AspNetCore.Components/Release/net10.0/Microsoft.AspNetCore.Components.dll` (from project root) to the NuGet package dir

In a script (run from the `Piral.Blazor` repository root):

```bash
#!/usr/bin/env bash
set -euo pipefail

PIRAL_BLAZOR_SOURCE="$(pwd)"
ASPNETCORE_TAG="${1:-v10.0.14}"
ASPNETCORE_VERSION="${ASPNETCORE_TAG#v}"
NUGET_PACKAGES_DIR="${NUGET_PACKAGES:-$(dotnet nuget locals global-packages --list | sed 's/^global-packages: //')}"

git clone https://github.com/dotnet/aspnetcore.git
cd aspnetcore
git checkout "$ASPNETCORE_TAG"
git apply "$PIRAL_BLAZOR_SOURCE/tools/componentfactory-piral.patch"

cd src/Components
./build.sh

cd Components/src
dotnet build Microsoft.AspNetCore.Components.csproj -c Release

cd ../../../..
cp artifacts/bin/Microsoft.AspNetCore.Components/Release/net10.0/Microsoft.AspNetCore.Components.dll \
  "$PIRAL_BLAZOR_SOURCE/tools/Microsoft.AspNetCore.Components.dll"
```

It could be that either a global SDK needs to be installed or that the global.json of the ASP.NET Core repository needs to be adjusted.
