# Piral.Blazor

[![Build Status](https://florianrappl.visualstudio.com/Piral.Blazor/_apis/build/status/FlorianRappl.Piral.Blazor?branchName=master)](https://florianrappl.visualstudio.com/Piral.Blazor/_build/latest?definitionId=27&branchName=master)

All things to make Blazor work seamlessly in microfrontends using Piral.

## Template

Make sure that you've installed the template.

```sh
dotnet new -i Piral.Blazor.Template
```

Create a new folder and run the template.

```sh
dotnet new blazorpilet
```

## Getting Started

We are still in the process of making this accessible. Any contribution appreciated!

(You'll also find some information on the [piral-blazor](https://www.npmjs.com/package/piral-blazor) package.)

## Usage

You can expose your Blazor components using the `ExposePilet` attribute.

```cs
@attribute [ExposePilet("counter")]

<h1>Counter from Lib A</h1>

<p>Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    int currentCount = 0;

    void IncrementCount()
    {
        currentCount++;
    }
}
```

You can define services for dependency injection in a `Module` class. Actually, the name of the class is arbitrary, but it shows the difference to the standard `Program` class, which should not be available.

To be able to compile it successfully a `Main` method should be declared, which should remain empty.

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

Use the `ConfigureServices` function to define dependency injection.
