using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Aplicacion.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Mesasitec.Api.Controllers;


[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IServicioAuth _auth;

    // Se inyecta el servicio de login (la interfaz, no la implementación concreta).
    public AuthController(IServicioAuth auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var resultado = await _auth.LoginAsync(request);

        // Credenciales incorrectas -> el servicio devolvió null -> 401 NO_AUTENTICADO.
        if (resultado is null)
        {
            return Unauthorized(new
            {
                type = "https://mesasitec.local/errores/no-autenticado",
                title = "No autenticado",
                status = 401,
                detail = "Credenciales incorrectas.",
                codigo = "NO_AUTENTICADO",
            });
        }

        return Ok(resultado);
    }
}