using Mesasitec.Aplicacion.Contratos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesasitec.Aplicacion.DTOs;

namespace Mesasitec.Api.Controllers;

[Route("api/v1/solicitudes")]
[Authorize]  // Todos los endpoints de solicitudes exigen token.
public class SolicitudesController : ApiControllerBase
{
    private readonly IServicioSolicitudes _servicio;

    public SolicitudesController(IServicioSolicitudes servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] SolicitudFiltros filtros)
    {
        var resultado = await _servicio.ListarAsync(
            TenantIdActual, UsuarioIdActual, RolActual, filtros);
        return Ok(resultado);
    }
}