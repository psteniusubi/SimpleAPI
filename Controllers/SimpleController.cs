using Microsoft.AspNetCore.Mvc;
using SimpleAPI.OIDC;

namespace SimpleAPI.Controllers
{
    [Route("simple")]
    [ApiController]
    public class SimpleController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index([FromHeader(Name = "Authorization")] string authorization)
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
}
