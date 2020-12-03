using System.Text.Json.Serialization;

namespace SimpleAPI.OIDC
{
    public class IntrospectionResponseModel
    {
        [JsonPropertyName("active")]
        public bool? Active { get; set; }
        [JsonPropertyName("sub")]
        public string Subject { get; set; }
    }
}
