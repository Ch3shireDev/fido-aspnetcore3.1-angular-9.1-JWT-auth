using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/secret")]
    [ApiController]
    [Authorize]
    public class SecretController : ControllerBase
    {
        public IActionResult Get()
        {
            return Ok(new {secret = "super secret info from api"});
        }
    }
}