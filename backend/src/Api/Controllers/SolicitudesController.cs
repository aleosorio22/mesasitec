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
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearSolicitudRequest request)
    {
        var detalle = await _servicio.CrearAsync(TenantIdActual, UsuarioIdActual, request);

        // Categoría inexistente o de otra organización -> el servicio devolvió null.
        if (detalle is null)
        {
            return UnprocessableEntity(new
            {
                type = "https://mesasitec.local/errores/validacion",
                title = "Validación",
                status = 422,
                detail = "La categoría no existe o no pertenece a su organización.",
                codigo = "VALIDACION",
            });
        }

        // 201 Created con la cabecera Location apuntando al nuevo recurso.
        return CreatedAtAction(
            actionName: nameof(ObtenerPorId),
            routeValues: new { id = detalle.Id },
            value: detalle);
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var detalle = await _servicio.ObtenerDetalleAsync(
            TenantIdActual, id, UsuarioIdActual, RolActual);

        // No existe, es de otra organización, o un Solicitante intenta ver una ajena -> 404.
        if (detalle is null)
        {
            return NotFound(new
            {
                type = "https://mesasitec.local/errores/recurso-no-encontrado",
                title = "Recurso no encontrado",
                status = 404,
                detail = "La solicitud no existe.",
                codigo = "RECURSO_NO_ENCONTRADO",
            });
        }

        return Ok(detalle);
    }
}