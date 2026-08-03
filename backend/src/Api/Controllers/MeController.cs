using System.Security.Claims;
using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Api.Errores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mesasitec.Api.Controllers;

[Route("api/v1/me")]
[Authorize]  // 🔒 Exige token válido. Sin él -> 401 automático (lo pone la maquinaria JWT).
public class MeController : ApiControllerBase
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

        if (!Guid.TryParse(sub, out var usuarioId))
            return Problema(ErroresApi.NoAutenticado("Token inválido."));

        var perfil = await _auth.ObtenerPerfilAsync(usuarioId);

        // El token es válido pero el usuario ya no existe o está inactivo -> 401.
        if (perfil is null)
            return Problema(ErroresApi.NoAutenticado("Usuario no encontrado o inactivo."));

        return Ok(perfil);
    }
}