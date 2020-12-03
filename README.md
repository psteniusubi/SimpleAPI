# API protection with OAuth 2.0 and Ubisecure SSO 

Example of a simple OAuth 2.0 protected API. Token introspection is used in this example to validate OAuth 2.0 bearer tokens.

There are two different solutions in this repository. One implemented with ASP.NET Core and an other implemented with Apache HTTP server and [mod_auth_openidc](https://github.com/zmartzone/mod_auth_openidc).

Examples of clients invoking this api are
* [SimpleSPA](../../../SimpleSPA) - JavaScript Single Page application

## ASP.NET Core Configuration

An OAuth 2.0 Client needs to be configured with information about the OAuth Provider and client credentials. This sample app puts these configuration items into `appsettings.json` file as properties of `OAuth2` key:

* `issuer` - name of OAuth Provider
* `client_id` and `client_secret` - client credentials registered with OAuth Provider

```json
{
  "OAuth2": {
    "issuer": "https://login.example.ubidemo.com/uas",
    "client_id": "api",
    "client_secret": "secret"
  }
}  
```

## ASP.NET Core Code review

Most of the project was generated with Visual Studio. The relevant new or modified files are
* [Startup.cs](Startup.cs)
* [SimpleController.cs](Controllers/SimpleController.cs)
* [IntrospectionClient.cs](OIDC/IntrospectionClient.cs)

This implementation shows what steps are required to create an OAuth 2.0 protected API. A real world application should re-factor token introspection into a middleware component and implement caching of introspection results to improve performance.

### Startup.cs

Here I'm adding dependency injection service with `AddHttpClient` and `AddSingleton<IntrospectionClient>`. Then I use `AddCors` to setup a default CORS policy.

```c#
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient<HttpClient>();
            services.AddSingleton<IntrospectionClient>();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .WithHeaders(HeaderNames.Authorization)
                        .WithExposedHeaders(HeaderNames.WWWAuthenticate));
            });
            services.AddControllers();
        }
```        

```c#
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseCors();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
```

### SimpleController.cs

The API controller gets `IntrospectionClient` from dependency injection. For each API request I'm validating the `Authorization` header with `ValidateAuthorization`.

```c#
    [Route("simple")]
    [ApiController]
    public class SimpleController : ControllerBase
    {
        public IntrospectionClient Client { get; }
        public SimpleController(IntrospectionClient client)
        {
            Client = client;
        }
        [HttpGet]
        public async Task<IActionResult> Index([FromHeader(Name = "Authorization")] string authorization)
        {
            var introspection = await Client.ValidateAuthorization(authorization);
            if (introspection != null)
            {
                var sub = introspection.Subject;
                var obj = new
                {
                    hello = sub
                };
                return new JsonResult(obj);
            }
            else
            {
                return new BearerTokenResult(Client.ClientId);
            }
        }
    }
```

### IntrospectionClient.cs

`IntrospectionClient` gets configuration parameters and http client from dependency injection.

```c#
        public IntrospectionClient(IConfiguration configuration, IHttpClientFactory factory)
        {
            var section = configuration.GetSection("OAuth2");
            if (section == null) throw new ApplicationException($"{nameof(IntrospectionClient)}: Missing configuration OAuth2");
            Issuer = section.GetValue<string>("issuer");
            ClientId = section.GetValue<string>("client_id");
            ClientSecret = section.GetValue<string>("client_secret");
            Http = factory.CreateClient();
        }
```

This reads OAuth 2.0 Server Metadata from a well-known address

```c#
        public async Task<OAuth2ServerMetadataModel> GetConfiguration()
        {
            var stream = await Http.GetStreamAsync(Issuer + "/.well-known/oauth-authorization-server");
            return await JsonSerializer.DeserializeAsync<OAuth2ServerMetadataModel>(stream);
        }
```

This creates OAuth 2.0 Token Introspection request

```c#
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
```

Invoking Token Introspection request

```c#
        public async Task<IntrospectionResponseModel> InvokeIntrospectionRequest(string token)
        {
            var metadata = await GetConfiguration();
            var httpRequest = NewIntrospectionRequest(metadata.IntrospectionEndpoint, token);
            var httpResponse = await Http.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode) return default;
            var stream = await httpResponse.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<IntrospectionResponseModel>(stream);
        }
```

This method is used by API controller to validate any token in `Authorization` header

```c#
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
```

## Apache HTTP server and [mod_auth_openidc](https://github.com/zmartzone/mod_auth_openidc)

### CORS setup

The following detects CORS simple request and CORS preflight request. For both CORS requests the `Access-Control-Allow-Origin` and `Access-Control-Expose-Headers` response headers are set. For preflight request in addition the `Access-Control-Allow-Headers` header is set and a `204 No Content` response is sent.

See also https://www.w3.org/TR/cors/

```
<If "-n %{HTTP:Origin}">
    Header always set Access-Control-Allow-Origin "*"
    Header always set Access-Control-Expose-Headers "WWW-Authenticate"
    <If "%{REQUEST_METHOD} == 'OPTIONS' && -n %{HTTP:Access-Control-Request-Method}">
        Header always set Access-Control-Allow-Headers "Authorization"
        Redirect 204
    </If>
</If>
```

### OAuth 2.0 resource server

A minimal configuration of mod_auth_openidc, in OAuth 2.0 resource server mode, needs token introspection endpoint and OAuth 2.0 client credentials.

```
OIDCOAuthIntrospectionEndpoint https://login.example.ubidemo.com/uas/oauth2/introspection

OIDCOAuthClientID api
OIDCOAuthClientSecret secret
```

### OAuth 2.0 protected API handler

OAuth 2.0 resource server integration is declared with `AuthType oauth20`. 

```
<Location "/">

    AuthType oauth20
    Require valid-user

</Location>

Alias /simple ${InstanceRoot}/hello.json
```

## Running the applications

This application is ready to run with Ubisecure SSO at login.example.ubidemo.com.

### With Azure WebSites

1. Use a client to invoke the API (https://ubi-simple-api.azurewebsites.net/simple)

### With ASP.NET Core

1. Clone this repository
1. Install ASP.NET Core runtime from https://www.microsoft.com/net/download
1. Use `dotnet run` to run the SimpleAPI application
1. Use a client to invoke the API (http://localhost:5001/simple)

### With Apache HTTP server

1. Clone this repository
1. Install Apache HTTP server
1. Install mod_auth_openidc from https://github.com/zmartzone/mod_auth_openidc/releases
1. Use `run-apache.cmd` on Windows or `./run-apache.sh` on Linux to start Apache HTTP server
1. Use a client to invoke the API ((http://localhost:5001/simple)

