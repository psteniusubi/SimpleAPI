using Microsoft.AspNetCore.Mvc;
using SimpleAPI.OIDC;
using System.Threading.Tasks;
using static SimpleAPI.OIDC.IntrospectionClient;

namespace SimpleAPI.Controllers
{
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
}
