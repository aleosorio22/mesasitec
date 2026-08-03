using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Aplicacion.DTOs;
using Mesasitec.Api.Errores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mesasitec.Api.Controllers;

[Route("api/v1/solicitudes")]
[Authorize]
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
        if (filtros.Page < 1 || filtros.PageSize > 100 || filtros.PageSize < 1)
            return Problema(ErroresApi.ParametroInvalido(
                "page debe ser >= 1 y pageSize debe estar entre 1 y 100."));

        var resultado = await _servicio.ListarAsync(
            TenantIdActual, UsuarioIdActual, RolActual, filtros);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var detalle = await _servicio.ObtenerDetalleAsync(
            TenantIdActual, id, UsuarioIdActual, RolActual);

        if (detalle is null)
            return Problema(ErroresApi.RecursoNoEncontrado("La solicitud no existe."));

        return Ok(detalle);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearSolicitudRequest request)
    {
        var detalle = await _servicio.CrearAsync(TenantIdActual, UsuarioIdActual, request);

        if (detalle is null)
            return Problema(ErroresApi.Validacion(
                "La categoría no existe o no pertenece a su organización."));

        return CreatedAtAction(
            actionName: nameof(ObtenerPorId),
            routeValues: new { id = detalle.Id },
            value: detalle);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarSolicitudRequest request)
    {
        var (resultado, detalle) = await _servicio.EditarAsync(
            TenantIdActual, id, UsuarioIdActual, RolActual, request);

        return resultado switch
        {
            ResultadoEdicion.Ok => Ok(detalle),
            ResultadoEdicion.NoEncontrada => Problema(ErroresApi.RecursoNoEncontrado("La solicitud no existe.")),
            // RN-03: intentar editar fuera del estado que permite el rol
            // es "algo que su rol no permite" -> 403 OPERACION_NO_PERMITIDA.
            ResultadoEdicion.EstadoNoEditable => Problema(ErroresApi.OperacionNoPermitida(
                "Su rol no permite editar la solicitud en su estado actual.")),
            ResultadoEdicion.CategoriaInvalida => Problema(ErroresApi.Validacion(
                "La categoría no existe o no pertenece a su organización.")),
            _ => Problema(ErroresApi.Interno()),
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
            ResultadoTransicion.NoEncontrada => Problema(ErroresApi.RecursoNoEncontrado("La solicitud no existe.")),
            ResultadoTransicion.NoPermitida => Problema(ErroresApi.OperacionNoPermitida()),
            ResultadoTransicion.TransicionInvalida => Problema(ErroresApi.TransicionInvalida(
                "La acción no es válida para el estado actual de la solicitud.")),
            ResultadoTransicion.AgenteInvalido => Problema(ErroresApi.AgenteInvalido()),
            ResultadoTransicion.MotivoRequerido => Problema(ErroresApi.MotivoRequerido(
                "La acción requiere un motivo con la longitud mínima.")),
            _ => Problema(ErroresApi.Interno()),
        };
    }
}