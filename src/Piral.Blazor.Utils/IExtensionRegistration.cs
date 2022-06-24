using System.Text.Json.Serialization;

namespace Piral.Blazor.Utils
{
    /// <summary>
    /// The DTO representing an extension registration.
    /// </summary>
    public class ExtensionRegistration
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("pilet")]
        public string Pilet { get; set; }

        [JsonPropertyName("defaults")]
        public object Defaults { get; set; }
    }
}
