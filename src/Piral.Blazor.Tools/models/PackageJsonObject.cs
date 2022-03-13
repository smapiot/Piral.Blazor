namespace Piral.Blazor.Tools.Models
{
    public class PiralSectionObject
    {
        public string Name { get; set; }
    }

    public class PackageJsonObject
    {
        public string Version { get; set; }

        public string Name { get; set; }

        public PiralSectionObject Piral { get; set; }
    }
}