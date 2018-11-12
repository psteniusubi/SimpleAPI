# API protection with OAuth 2.0 and Ubisecure SSO 

Example of a simple OAuth 2.0 protected API. Token introspection is used in this example to validate OAuth 2.0 bearer tokens.

There are two different solutions in this repository. One implemented with ASP.NET Core and an other implemented with Apache HTTP server and [mod_auth_openidc](https://github.com/zmartzone/mod_auth_openidc).

## ASP.NET Core

The project was generated with Visual Studio 2017. The relevant modified files are
* [Startup.cs](Startup.cs)
* [Client.cs](OIDC/Client.cs)
* [SimpleController.cs](Controllers/SimpleController.cs)

This implementation shows what steps are required to create an OAuth 2.0 protected API. A real world application should re-factor token introspection into a middleware component and implement caching of introspection results to improve performance.

### CORS setup

I'm using the [CORS middleware](https://docs.microsoft.com/en-us/aspnet/core/security/cors) of ASP.NET Core for setting the necessary CORS headers.

```c#
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .WithHeaders(HeaderNames.Authorization)
                        .WithExposedHeaders(HeaderNames.WWWAuthenticate));
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }
```

```c#
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors();
            app.UseMvc();
        }
```

Note that `AddCors` and `UseCors` must appear before `AddMvc` and `UseMvc`.

### API controller

The API method invokes `Client.TryValidateAuthorization` with the HTTP Authorization request header. If validation fails then `Client.BearerToken` is invoked to send a 401 response with a WWW-Authenticate response header.

```c#
    [Route("simple")]
    [ApiController]
    public class SimpleController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index([FromHeader(Name = HeaderNames.Authorization)] string authorization)
        {
            if (Client.TryValidateAuthorization(authorization, out var introspection))
            {
                var sub = (string)introspection["sub"];
                var obj = new
                {
                    hello = sub
                };
                return new JsonResult(obj);
            }
            else
            {
                return Client.BearerToken(Client.API_CLIENT.ClientId);
            }
        }
    }
```

### OAuth 2.0 token introspection 

The `TryValidateAuthorization` method first verifies syntax of Authorization header, then invokes token introspection and finally checks introspection response was positive by checking boolean `active` response parameter has value `true`.

```c#
        public static bool TryValidateAuthorization(string authorization, out JsonObject introspection)
        {
            introspection = null;
            if (string.IsNullOrWhiteSpace(authorization))
            {
                return false;
            }
            AuthenticationHeaderValue header = AuthenticationHeaderValue.Parse(authorization);
            if (header == null || string.IsNullOrWhiteSpace(header.Parameter) || "Bearer" != header.Scheme)
            {
                return false;
            }
            var task = Client.InvokeIntrospectionRequest(header.Parameter);
            task.Wait();
            if (task.IsCompletedSuccessfully && task.Result["active"])
            {
                introspection = task.Result;
                return true;
            }
            else
            {
                return false;
            }
        }
```

This method returns the OAuth 2.0 provider metadata as a `JsonObject`

```c#
        public static async Task<JsonObject> GetConfiguration(string issuer = ISSUER)
        {
            return await Http
                .GetStringAsync(issuer + "/.well-known/openid-configuration")
                .ContinueWith(task => JsonValue.Parse(task.Result) as JsonObject);
        }
```

Here we create the OAuth 2.0 introspection request. 

```c#
        public static HttpRequestMessage NewIntrospectionRequest(string introspectionEndpoint, string token)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, introspectionEndpoint);
            httpRequest.Headers.Authorization = NewHttpBasic(API_CLIENT.ClientId, API_CLIENT.ClientSecret);
            var introspectionRequest = new Dictionary<string, string>();
            introspectionRequest["token"] = token;
            httpRequest.Content = new FormUrlEncodedContent(introspectionRequest);
            return httpRequest;
        }
```

Here the introspection request is invoked and the response is returned as a `JsonObject`.

```c#
        public static async Task<JsonObject> InvokeIntrospectionRequest(string token)
        {
            var metadata = await GetConfiguration();
            var httpRequest = NewIntrospectionRequest(metadata["introspection_endpoint"], token);
            var introspectionResponse = await Http.SendAsync(httpRequest)
                .ContinueWith(task => task.Result.Content.ReadAsStringAsync())
                .Unwrap()
                .ContinueWith(task => JsonValue.Parse(task.Result) as JsonObject);
            return introspectionResponse;
        }
```

## Apache HTTP server and [mod_auth_openidc](https://github.com/zmartzone/mod_auth_openidc)

### CORS setup

```
<If "-n %{HTTP:Origin}">
    Header always set Access-Control-Allow-Origin "*"
    Header always set Access-Control-Expose-Headers "WWW-Authenticate"
    #Header always set Access-Control-Max-Age "0"
    <If "%{REQUEST_METHOD} == 'OPTIONS' && -n %{HTTP:Access-Control-Request-Method}">
        Header always set Access-Control-Allow-Headers "Authorization"
        Redirect 204
    </If>
</If>
```

### OAuth 2.0 resource server

```
OIDCOAuthIntrospectionEndpoint https://login.example.ubidemo.com/uas/oauth2/introspection

OIDCOAuthClientID api
OIDCOAuthClientSecret secret
```

### OAuth 2.0 protected API handler

```
<Location "/">

    AuthType oauth20
    Require valid-user

</Location>

Alias /simple ${InstanceRoot}/hello.json
```

## Running the application

### With ASP.NET Core

1. `dotnet run`

### With Apache HTTP server

1. `run-apache.cmd` or `./run-apache.sh`
