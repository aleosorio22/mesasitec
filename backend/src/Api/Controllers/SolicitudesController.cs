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
        // Validación de parámetros de paginación (§6.2). Fuera de rango -> 400 PARAMETRO_INVALIDO.
        if (filtros.Page < 1 || filtros.PageSize > 100 || filtros.PageSize < 1)
        {
            return BadRequest(new
            {
                type = "https://mesasitec.local/errores/parametro-invalido",
                title = "Parámetro inválido",
                status = 400,
                detail = "page debe ser >= 1 y pageSize debe estar entre 1 y 100.",
                codigo = "PARAMETRO_INVALIDO",
            });
        }

        var resultado = await _servicio.ListarAsync(
            TenantIdActual, UsuarioIdActual, RolActual, filtros);
        return Ok(resultado);
    }
}