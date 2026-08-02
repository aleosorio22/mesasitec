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
}