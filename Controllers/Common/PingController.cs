using Microsoft.AspNetCore.Mvc;

namespace FeeNominalService.Controllers.Common
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