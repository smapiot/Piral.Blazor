using Piral.Blazor.Utils;
using System;
using System.Reflection;

namespace Piral.Blazor.Core;

internal class PiletData
{
    public Assembly Library { get; set; }

    public PiletService Service { get; set; }

    public EventHandler LanguageHandler { get; set; }

    public PiletDefinition Definition { get; set; }

    public IServiceProvider Provider { get; set; }
}