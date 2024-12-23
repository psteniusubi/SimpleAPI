using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;

namespace SimpleAPI.OIDC
{
    public class IntrospectionClient
    {
        public IntrospectionClient(IConfiguration configuration, IHttpClientFactory factory)
        {
            var section = configuration.GetSection("OAuth2");
            if (section == null) throw new ApplicationException($"{nameof(IntrospectionClient)}: Missing configuration OAuth2");
            Issuer = section.GetValue<string>("issuer");
            ClientId = section.GetValue<string>("client_id");
            ClientSecret = section.GetValue<string>("client_secret");
            Http = factory.CreateClient();
        }

        public string Issuer { get; }
        public string ClientId { get; }
        public string ClientSecret { get; }
        public HttpClient Http { get; }

        public async Task<OAuth2ServerMetadataModel> GetConfiguration()
        {
            var stream = await Http.GetStreamAsync(Issuer + "/.well-known/oauth-authorization-server");
            return await JsonSerializer.DeserializeAsync<OAuth2ServerMetadataModel>(stream);
        }

        public AuthenticationHeaderValue NewBasicAuthenticationHeader(string username, string password)
        {
            var bytes = Encoding.UTF8.GetBytes(string.Join(":", username, password));
            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
        }

        public HttpRequestMessage NewIntrospectionRequest(string introspectionEndpoint, string token)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, introspectionEndpoint);
            httpRequest.Headers.Authorization = NewBasicAuthenticationHeader(ClientId, ClientSecret);
            var introspectionRequest = new Dictionary<string, string>
            {
                ["token"] = token
            };
            httpRequest.Content = new FormUrlEncodedContent(introspectionRequest);
            return httpRequest;
        }

        public async Task<IntrospectionResponseModel> InvokeIntrospectionRequest(string token)
        {
            var metadata = await GetConfiguration();
            var httpRequest = NewIntrospectionRequest(metadata.IntrospectionEndpoint, token);
            var httpResponse = await Http.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode) return default;
            var stream = await httpResponse.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<IntrospectionResponseModel>(stream);
        }

        public bool TryParseBearerAuthorization(string authorization, out AuthenticationHeaderValue result)
            => AuthenticationHeaderValue.TryParse(authorization, out result)
                && !string.IsNullOrEmpty(result.Parameter)
                && string.Equals("Bearer", result.Scheme, StringComparison.InvariantCultureIgnoreCase);

        public async Task<IntrospectionResponseModel> ValidateAuthorization(string authorization)
        {
            if (!TryParseBearerAuthorization(authorization, out var header))
            {
                return default;
            }
            var introspection = await InvokeIntrospectionRequest(header.Parameter);
            if (introspection?.Active == true)
            {
                return introspection;
            }
            else
            {
                return default;
            }
        }
    }
}
