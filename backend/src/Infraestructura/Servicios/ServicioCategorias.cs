using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Aplicacion.DTOs;
using Mesasitec.Infraestructura.Data;
using Microsoft.EntityFrameworkCore;

namespace Mesasitec.Infraestructura.Servicios;

public class ServicioCategorias : IServicioCategorias
{
    private readonly MesaSitecDbContext _db;

    public ServicioCategorias(MesaSitecDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoriaDto>> ListarActivasAsync(Guid tenantId)
    {
        return await _db.Categorias
            .Where(c => c.TenantId == tenantId && c.Activo)   // RN-01 + solo activas
            .OrderBy(c => c.Nombre)                            // orden alfabético, prolijo
            .Select(c => new CategoriaDto(c.Id, c.Nombre, c.SlaHoras))
            .ToListAsync();
    }
}