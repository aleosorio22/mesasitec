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
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarSolicitudRequest request)
    {
        var (resultado, detalle) = await _servicio.EditarAsync(
            TenantIdActual, id, UsuarioIdActual, RolActual, request);

        return resultado switch
        {
            ResultadoEdicion.Ok => Ok(detalle),

            ResultadoEdicion.NoEncontrada => NotFound(new
            {
                type = "https://mesasitec.local/errores/recurso-no-encontrado",
                title = "Recurso no encontrado",
                status = 404,
                detail = "La solicitud no existe.",
                codigo = "RECURSO_NO_ENCONTRADO",
            }),

            ResultadoEdicion.EstadoNoEditable => Conflict(new
            {
                type = "https://mesasitec.local/errores/conflicto-estado",
                title = "Conflicto de estado",
                status = 409,
                detail = "Solo se pueden editar solicitudes en estado Nueva o Asignada.",
                codigo = "CONFLICTO_ESTADO",
            }),

            ResultadoEdicion.CategoriaInvalida => UnprocessableEntity(new
            {
                type = "https://mesasitec.local/errores/validacion",
                title = "Validación",
                status = 422,
                detail = "La categoría no existe o no pertenece a su organización.",
                codigo = "VALIDACION",
            }),

            _ => StatusCode(500),
        };
    }
    [HttpPost("{id:guid}/transiciones")]
    public async Task<IActionResult> Transicionar(Guid id, [FromBody] TransicionRequest request)
    {
        var (resultado, detalle) = await _servicio.EjecutarTransicionAsync(
            TenantIdActual, UsuarioIdActual, RolActual, id, request);

        return resultado switch
        {
            ResultadoTransicion.Ok => Ok(detalle),

            ResultadoTransicion.NoEncontrada => NotFound(new
            {
                type = "https://mesasitec.local/errores/recurso-no-encontrado",
                title = "Recurso no encontrado",
                status = 404,
                detail = "La solicitud no existe.",
                codigo = "RECURSO_NO_ENCONTRADO",
            }),

            ResultadoTransicion.NoPermitida => StatusCode(403, new
            {
                type = "https://mesasitec.local/errores/operacion-no-permitida",
                title = "Operación no permitida",
                status = 403,
                detail = "Su rol no permite ejecutar esta acción.",
                codigo = "OPERACION_NO_PERMITIDA",
            }),

            ResultadoTransicion.TransicionInvalida => Conflict(new
            {
                type = "https://mesasitec.local/errores/transicion-invalida",
                title = "Transición inválida",
                status = 409,
                detail = "La acción no es válida para el estado actual de la solicitud.",
                codigo = "TRANSICION_INVALIDA",
            }),

            ResultadoTransicion.AgenteInvalido => UnprocessableEntity(new
            {
                type = "https://mesasitec.local/errores/agente-invalido",
                title = "Agente inválido",
                status = 422,
                detail = "El agente no existe, no está activo, o no pertenece a su organización.",
                codigo = "AGENTE_INVALIDO",
            }),

            ResultadoTransicion.MotivoRequerido => UnprocessableEntity(new
            {
                type = "https://mesasitec.local/errores/motivo-requerido",
                title = "Motivo requerido",
                status = 422,
                detail = "La acción requiere un motivo con la longitud mínima.",
                codigo = "MOTIVO_REQUERIDO",
            }),

            _ => StatusCode(500),
        };
    }
}