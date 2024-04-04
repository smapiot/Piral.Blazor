# Patch Instructions

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
