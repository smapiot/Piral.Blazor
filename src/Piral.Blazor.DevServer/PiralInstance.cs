namespace Piral.Blazor.DevServer;

public class PiralInstance
{
    public PiralInstance(string name, bool website)
    {
        Name = name;
        IsWebsite = website;
    }

    public string Name { get; }

    public bool IsWebsite { get; }
}
