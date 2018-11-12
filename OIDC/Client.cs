using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SimpleAPI.OIDC
{
    public static class Client
    {
        public class Config
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string RedirectUri { get; set; }
        }

        public const string ISSUER = "https://login.example.ubidemo.com/uas";

        public static Config API_CLIENT { get; } = new Config
        {
            ClientId = "api",
            ClientSecret = "secret",
            RedirectUri = null,
        };

        public static HttpClient Http { get; } = new HttpClient();

        public static async Task<JsonObject> GetConfiguration(string issuer = ISSUER)
        {
            return await Http
                .GetStringAsync(issuer + "/.well-known/openid-configuration")
                .ContinueWith(task => JsonValue.Parse(task.Result) as JsonObject);
        }

        public static AuthenticationHeaderValue NewHttpBasic(string username, string password)
        {
            var bytes = Encoding.UTF8.GetBytes(string.Join(":", username, password));
            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
        }

        public static HttpRequestMessage NewIntrospectionRequest(string introspectionEndpoint, string token)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, introspectionEndpoint);
            httpRequest.Headers.Authorization = NewHttpBasic(API_CLIENT.ClientId, API_CLIENT.ClientSecret);
            var introspectionRequest = new Dictionary<string, string>();
            introspectionRequest["token"] = token;
            httpRequest.Content = new FormUrlEncodedContent(introspectionRequest);
            return httpRequest;
        }

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

        public static BearerTokenResult BearerToken(string scope)
        {
            return new BearerTokenResult()
            {
                Scope = scope
            };
        }

        public class BearerTokenResult : UnauthorizedResult
        {
            public string Scope { get; set; }
            public override Task ExecuteResultAsync(ActionContext context)
            {
                return base.ExecuteResultAsync(context)
                    .ContinueWith(task =>
                    {
                        var bearer = "Bearer";
                        if (!string.IsNullOrWhiteSpace(Scope) && Scope != "openid")
                        {
                            bearer += " realm=\"" + Scope + "\"";
                            bearer += " scope=\"openid " + Scope + "\"";
                        }
                        else
                        {
                            bearer += " scope=\"openid\"";
                        }
                        context.HttpContext.Response.Headers.Add(HeaderNames.WWWAuthenticate, bearer);
                        return task;
                    })
                    .Unwrap();
            }
        }

    }
}
