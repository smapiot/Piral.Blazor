using System.Text.Json.Serialization;

namespace Piral.Blazor.DevServer;

public class PackageJson
{
    [JsonPropertyName("piral")]
    public PiralSection? Piral { get; set; }

    [JsonPropertyName("piralCLI")]
    public PiralCliSection? PiralCli { get; set; }

    [JsonPropertyName("files")]
    public List<string>? Files { get; set; }

    public class PiralSection
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class PiralCliSection
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("generated")]
        public bool? Generated { get; set; }
    }
}
