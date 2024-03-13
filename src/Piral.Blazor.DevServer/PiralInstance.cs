namespace Piral.Blazor.DevServer;

public class PiralInstance(string name, bool website)
{
    public string Name { get; } = name;

    public bool IsWebsite { get; } = website;
}
