using System.Security.Claims;
using Mesasitec.Aplicacion.Contratos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mesasitec.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]  // 🔒 Exige token válido. Sin él -> 401 automático (lo pone la maquinaria JWT).
public class MeController : ControllerBase
{
    private readonly IServicioAuth _auth;

    public MeController(IServicioAuth auth)
    {
        _auth = auth;
    }

    [HttpGet]
    public async Task<IActionResult> GetPerfil()
    {
        // El "sub" del token trae el id del usuario. Lo leemos de los claims validados.
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        // Si por alguna razón no hay sub o no es un Guid válido -> 401.
        if (!Guid.TryParse(sub, out var usuarioId))
            return Unauthorized(new
            {
                type = "https://mesasitec.local/errores/no-autenticado",
                title = "No autenticado",
                status = 401,
                detail = "Token inválido.",
                codigo = "NO_AUTENTICADO",
            });

        var perfil = await _auth.ObtenerPerfilAsync(usuarioId);

        // El token es válido pero el usuario ya no existe o está inactivo -> 401.
        if (perfil is null)
            return Unauthorized(new
            {
                type = "https://mesasitec.local/errores/no-autenticado",
                title = "No autenticado",
                status = 401,
                detail = "Usuario no encontrado o inactivo.",
                codigo = "NO_AUTENTICADO",
            });

        return Ok(perfil);
    }
}