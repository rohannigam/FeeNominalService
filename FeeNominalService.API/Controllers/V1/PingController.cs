using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeeNominalService.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [ApiVersion("1.0")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("pong");
        }
    }
} 