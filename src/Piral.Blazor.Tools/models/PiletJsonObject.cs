using System.Collections.Generic;

namespace Piral.Blazor.Tools.Models
{
    public class PiralInstanceObject
    {
        public bool Selected { get; set; }
    }

    public class PiletJsonObject
    {
        public string SchemaVersion { get; set; }

        public Dictionary<string, PiralInstanceObject> PiralInstances { get; set; }
    }
}
