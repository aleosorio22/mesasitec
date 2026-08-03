using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Aplicacion.DTOs;
using Mesasitec.Api.Errores;
using Microsoft.AspNetCore.Mvc;

namespace Mesasitec.Api.Controllers;

[Route("api/v1/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IServicioAuth _auth;

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
            return Problema(ErroresApi.NoAutenticado("Credenciales incorrectas."));

        return Ok(resultado);
    }
}