using System.Security.Claims;
using Mesasitec.Dominio.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Mesasitec.Api.Controllers;

// Clase base para nuestros controllers. Centraliza la lectura del "contexto del usuario"
// desde los claims del token, para no repetir User.FindFirst(...) en cada endpoint.
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // El tenant del usuario autenticado (RN-01: filtro obligatorio en toda consulta).
    protected Guid TenantIdActual =>
        Guid.Parse(User.FindFirst("tenantId")!.Value);

    // El id del usuario autenticado (para "solo las que él creó", RN-03).
    protected Guid UsuarioIdActual =>
        Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")!.Value);

    // El rol del usuario autenticado (RN-03: permisos).
    protected Rol RolActual =>
        Enum.Parse<Rol>(User.FindFirst("rol")!.Value);
}