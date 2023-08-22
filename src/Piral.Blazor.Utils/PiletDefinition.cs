using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Piral.Blazor.Utils
{
    /// <summary>
    /// The DTO representing a pilet for loading.
    /// </summary>
	public class PiletDefinition
    {
        [JsonPropertyName("dllUrl")]
        public string DllUrl { get; set; }

        [JsonPropertyName("pdbUrl")]
        public string PdbUrl { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("config")]
        public string Config { get; set; }

        [JsonPropertyName("baseUrl")]
        public string BaseUrl { get; set; }

        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; }

        [JsonPropertyName("dependencySymbols")]
        public List<string> DependencySymbols { get; set; }

        [JsonPropertyName("satellites")]
        public Dictionary<string, List<string>> Satellites { get; set; }
    }
}
