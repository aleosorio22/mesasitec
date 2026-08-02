using Mesasitec.Aplicacion.DTOs;
using Mesasitec.Dominio.Enums;

namespace Mesasitec.Aplicacion.Contratos;

public interface IServicioSolicitudes
{
    Task<ResultadoPaginado<SolicitudListaDto>> ListarAsync(
        Guid tenantId,
        Guid usuarioId,
        Rol rol,
        SolicitudFiltros filtros);

    // Crea una solicitud. Devuelve el detalle, o null si la categoría no es válida del tenant.
    Task<SolicitudDetalleDto?> CrearAsync(
        Guid tenantId, Guid usuarioId, CrearSolicitudRequest request);
    
    // Detalle de una solicitud por id. null si no existe, es de otro tenant,
    // o un Solicitante intenta ver una que no creó (RN-01 + RN-03).
    Task<SolicitudDetalleDto?> ObtenerDetalleAsync(
        Guid tenantId, Guid id, Guid usuarioId, Rol rol);
}