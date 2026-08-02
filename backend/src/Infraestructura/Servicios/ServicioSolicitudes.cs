using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Aplicacion.DTOs;
using Mesasitec.Dominio.Enums;
using Mesasitec.Dominio.Reglas;
using Mesasitec.Infraestructura.Data;
using Microsoft.EntityFrameworkCore;

namespace Mesasitec.Infraestructura.Servicios;

public class ServicioSolicitudes : IServicioSolicitudes
{
    private readonly MesaSitecDbContext _db;

    public ServicioSolicitudes(MesaSitecDbContext db)
    {
        _db = db;
    }

    public async Task<ResultadoPaginado<SolicitudListaDto>> ListarAsync(
        Guid tenantId, Guid usuarioId, Rol rol, SolicitudFiltros filtros)
    {
        var ahora = DateTime.UtcNow;

        // 1. RN-01: filtro por tenant (sagrado, siempre primero).
        var query = _db.Solicitudes
            .Where(s => s.TenantId == tenantId);

        // 2. RN-03: el Solicitante solo ve las suyas.
        if (rol == Rol.Solicitante)
            query = query.Where(s => s.SolicitanteId == usuarioId);

        // 3. FILTROS: cada uno se aplica solo si el usuario lo mandó (no es null).
        if (filtros.Estado is not null)
            query = query.Where(s => s.Estado == filtros.Estado);

        if (filtros.Prioridad is not null)
            query = query.Where(s => s.Prioridad == filtros.Prioridad);

        if (filtros.CategoriaId is not null)
            query = query.Where(s => s.CategoriaId == filtros.CategoriaId);

        if (filtros.AgenteId is not null)
            query = query.Where(s => s.AgenteId == filtros.AgenteId);
        // Búsqueda de texto (q): en título, descripción o código, sin distinguir mayúsculas.
        if (!string.IsNullOrWhiteSpace(filtros.Q))
        {
            var termino = filtros.Q.Trim().ToLower();
            query = query.Where(s =>
                s.Titulo.ToLower().Contains(termino) ||
                s.Descripcion.ToLower().Contains(termino) ||
                s.Codigo.ToLower().Contains(termino));
        }

        // 4. Relaciones para el DTO.
        query = query
            .Include(s => s.Categoria)
            .Include(s => s.Agente);

        // 5. Total antes de paginar.
        var total = await query.CountAsync();

        // 6. Ejecuta.
        var solicitudes = await query
            .OrderByDescending(s => s.FechaCreacion)
            .ToListAsync();

        // 7. Mapeo a DTO.
        var items = solicitudes
            .Select(s => new SolicitudListaDto(
                s.Id,
                s.Codigo,
                s.Titulo,
                s.Estado.ToString(),
                s.Prioridad.ToString(),
                new CategoriaResumenDto(s.Categoria!.Id, s.Categoria.Nombre),
                s.Agente == null ? null : new AgenteResumenDto(s.Agente.Id, s.Agente.Nombre),
                s.FechaCreacion,
                s.FechaLimiteSla,
                CalculadoraSla.EstaVencida(s.FechaLimiteSla, s.Estado, ahora)))
            .ToList();

        // 8. Filtro "vencidas": OJO, va DESPUÉS del mapeo (ver explicación).
        if (filtros.Vencidas is not null)
            items = items.Where(i => i.Vencida == filtros.Vencidas).ToList();

        return new ResultadoPaginado<SolicitudListaDto>(
            Items: items,
            Page: 1,
            PageSize: items.Count,
            Total: filtros.Vencidas is not null ? items.Count : total,
            TotalPaginas: 1);
    }
}