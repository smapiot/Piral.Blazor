using System.Text.Json.Serialization;

namespace Piral.Blazor.DevServer;

public class PiletJson
{
    [JsonPropertyName("piralInstances")]
    public Dictionary<string, PiralInstanceReference>? PiralInstances { get; set; }

    public class PiralInstanceReference
    {
        [JsonPropertyName("selected")]
        public bool? IsSelected { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
