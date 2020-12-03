using System.Text.Json.Serialization;

namespace SimpleAPI.OIDC
{
    public class OAuth2ServerMetadataModel
    {
        [JsonPropertyName("introspection_endpoint")]
        public string IntrospectionEndpoint { get; set; }
    }
}
