using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Aplicacion.DTOs;
using Mesasitec.Dominio.Enums;
using Mesasitec.Infraestructura.Data;
using Microsoft.EntityFrameworkCore;

namespace Mesasitec.Infraestructura.Servicios;

public class ServicioUsuarios : IServicioUsuarios
{
    private readonly MesaSitecDbContext _db;

    public ServicioUsuarios(MesaSitecDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AgenteResumenDto>> ListarAgentesAsync(Guid tenantId)
    {
        // Mismas condiciones que valida RN-05 al asignar: activo, del tenant,
        // con rol Agente o Admin. Solo se exponen id y nombre.
        return await _db.Usuarios
            .Where(u => u.TenantId == tenantId
                     && u.Activo
                     && (u.Rol == Rol.Agente || u.Rol == Rol.Admin))
            .OrderBy(u => u.Nombre)
            .Select(u => new AgenteResumenDto(u.Id, u.Nombre))
            .ToListAsync();
    }
}
