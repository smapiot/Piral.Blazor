using System.Text.Json;
using System.Text.Json.Serialization;

namespace Piral.Blazor.DevServer;

public class PiletJson
{
    [JsonPropertyName("piralInstances")]
    public Dictionary<string, JsonElement>? PiralInstances { get; set; }
}
