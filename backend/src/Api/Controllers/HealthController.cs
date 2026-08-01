using Microsoft.AspNetCore.Mvc;

namespace Mesasitec.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetEstado()
    {
        return Ok(new { estado = "ok" });
    }
}