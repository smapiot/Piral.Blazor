using System.Text.Json.Serialization;

namespace Piral.Blazor.DevServer
{
    public class PackageJson
    {
        [JsonPropertyName("piral")]
        public PiralSection? Piral { get; set; }

        public class PiralSection
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }
    }
}
