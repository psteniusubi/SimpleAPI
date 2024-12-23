using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;

namespace SimpleAPI.OIDC
{
    public class BearerTokenResult : IActionResult
    {
        public string Scope { get; }
        public BearerTokenResult(string scope)
        {
            Scope = scope;
        }
        public Task ExecuteResultAsync(ActionContext context)
        {
            string parameter;
            if (!string.IsNullOrWhiteSpace(Scope) && Scope != "openid")
            {
                parameter = $"realm=\"{Scope}\", scope=\"openid {Scope}\"";
            }
            else
            {
                parameter = "scope=\"openid\"";
            }
            var header = new AuthenticationHeaderValue("Bearer", parameter);
            context.HttpContext.Response.StatusCode = 401;
            context.HttpContext.Response.Headers[HeaderNames.WWWAuthenticate] = header.ToString();
            return Task.CompletedTask;
        }
    }
}
