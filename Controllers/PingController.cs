using Microsoft.AspNetCore.Mvc;

namespace SurchargePOC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        [HttpPost]
        public IActionResult Get()
        {
            return Ok("pong");
        }
    }
}
