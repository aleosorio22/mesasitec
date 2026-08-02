using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Aplicacion.DTOs;
using Mesasitec.Dominio.Enums;
using Mesasitec.Dominio.Reglas;
using Mesasitec.Infraestructura.Data;
using Microsoft.EntityFrameworkCore;
using Mesasitec.Dominio.Entidades;

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
    public async Task<SolicitudDetalleDto?> CrearAsync(
        Guid tenantId, Guid usuarioId, CrearSolicitudRequest request)
    {
        var ahora = DateTime.UtcNow;

        // 1. Validar que la categoría exista, sea del tenant y esté activa (RN-01 aplicado aquí también).
        var categoria = await _db.Categorias
            .FirstOrDefaultAsync(c => c.Id == request.CategoriaId
                                   && c.TenantId == tenantId
                                   && c.Activo);

        // Categoría inexistente o de otra organización -> null (el controller da 404/422).
        if (categoria is null)
            return null;

        // 2. Generar el código correlativo (RN-07): contar las del tenant en el año actual + 1.
        var anio = ahora.Year;
        var cuantasEsteAnio = await _db.Solicitudes
            .CountAsync(s => s.TenantId == tenantId && s.FechaCreacion.Year == anio);
        var correlativo = cuantasEsteAnio + 1;
        var codigo = $"SOL-{anio}-{correlativo:D5}";

        // 3. Calcular el SLA (RN-04) con la calculadora del dominio.
        var fechaLimite = CalculadoraSla.CalcularFechaLimite(
            ahora, categoria.SlaHoras, request.Prioridad);

        // 4. Crear la entidad. El servidor fija estado=Nueva, solicitante, fechas.
        var solicitud = new Solicitud
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Codigo = codigo,
            Titulo = request.Titulo,
            Descripcion = request.Descripcion,
            CategoriaId = request.CategoriaId,
            Prioridad = request.Prioridad,
            Estado = Estado.Nueva,           // toda solicitud nace Nueva
            SolicitanteId = usuarioId,       // el usuario del token
            AgenteId = null,                 // aún sin agente
            FechaCreacion = ahora,
            FechaLimiteSla = fechaLimite,
        };

        _db.Solicitudes.Add(solicitud);
        await _db.SaveChangesAsync();

        // 5. Devolver el detalle completo reutilizando el método de detalle.
        return await ObtenerDetalleAsync(tenantId, solicitud.Id, usuarioId, rol: Rol.Admin);
    
    }
    // Mapea una entidad Solicitud a su DTO de detalle completo.
    // Requiere que Categoria, Solicitante y Agente (si hay) vengan cargados.
    private static SolicitudDetalleDto MapearDetalle(Solicitud s, DateTime ahora) =>
        new SolicitudDetalleDto(
            s.Id,
            s.Codigo,
            s.Titulo,
            s.Descripcion,
            s.Estado.ToString(),
            s.Prioridad.ToString(),
            new CategoriaResumenDto(s.Categoria!.Id, s.Categoria.Nombre),
            new SolicitanteResumenDto(s.Solicitante!.Id, s.Solicitante.Nombre),
            s.Agente == null ? null : new AgenteResumenDto(s.Agente.Id, s.Agente.Nombre),
            s.FechaCreacion,
            s.FechaLimiteSla,
            s.FechaResolucion,
            s.MotivoResolucion,
            s.MotivoCancelacion,
            CalculadoraSla.EstaVencida(s.FechaLimiteSla, s.Estado, ahora));
    public async Task<SolicitudDetalleDto?> ObtenerDetalleAsync(
        Guid tenantId, Guid id, Guid usuarioId, Rol rol)
    {
        var ahora = DateTime.UtcNow;

        var solicitud = await _db.Solicitudes
            .Include(s => s.Categoria)
            .Include(s => s.Solicitante)
            .Include(s => s.Agente)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);  // RN-01

        // No existe o es de otra organización -> null (el controller da 404).
        if (solicitud is null)
            return null;

        // RN-03: un Solicitante solo puede ver las que él creó.
        if (rol == Rol.Solicitante && solicitud.SolicitanteId != usuarioId)
            return null;

        return MapearDetalle(solicitud, ahora);
    }
}