using System.Text.Json.Serialization;

namespace SimpleAPI.OIDC
{
    public class OpenIDConfigurationModel
    {
        [JsonPropertyName("introspection_endpoint")]
        public string IntrospectionEndpoint { get; set; }
    }
}
