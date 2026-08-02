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

        // 5. Ordenamiento (§6.2). Default: -fechaCreacion (más recientes primero).
        //    Al ordenar por prioridad, el orden es SEMÁNTICO (Critica>Alta>Media>Baja):
        //    sale gratis porque el enum se guarda como número (Baja=0..Critica=3).
        query = filtros.Sort switch
        {
            "fechaCreacion"  => query.OrderBy(s => s.FechaCreacion),
            "-fechaCreacion" => query.OrderByDescending(s => s.FechaCreacion),
            "prioridad"      => query.OrderBy(s => s.Prioridad),           // Baja→Critica
            "-prioridad"     => query.OrderByDescending(s => s.Prioridad), // Critica→Baja
            "codigo"         => query.OrderBy(s => s.Codigo),
            _                => query.OrderByDescending(s => s.FechaCreacion), // default
        };

        //6 Ejecutar 
        var solicitudes = await query.ToListAsync();

        // 7. Mapeo a DTO (aquí se calcula "vencida" con el dominio).
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

        // 8. Filtro "vencidas" en memoria (vencida no es columna).
        if (filtros.Vencidas is not null)
            items = items.Where(i => i.Vencida == filtros.Vencidas).ToList();

        // 9. Total REAL: después de TODOS los filtros (incluido vencidas).
        var total = items.Count;

        // 10. Paginación (§6.2). Skip/Take sobre la lista final.
        var totalPaginas = (int)Math.Ceiling(total / (double)filtros.PageSize);
        var itemsPagina = items
            .Skip((filtros.Page - 1) * filtros.PageSize)
            .Take(filtros.PageSize)
            .ToList();

        return new ResultadoPaginado<SolicitudListaDto>(
            Items: itemsPagina,
            Page: filtros.Page,
            PageSize: filtros.PageSize,
            Total: total,
            TotalPaginas: totalPaginas);
    }
}