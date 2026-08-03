using Mesasitec.Aplicacion.Contratos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mesasitec.Api.Controllers;

[Route("api/v1/usuarios")]
[Authorize]
public class UsuariosController : ApiControllerBase
{
    private readonly IServicioUsuarios _servicio;

    public UsuariosController(IServicioUsuarios servicio)
    {
        _servicio = servicio;
    }

    // Endpoint EXTRA al contrato (declarado en DECISIONES.md): el modal de
    // 'asignar' del frontend necesita la lista de agentes válidos del tenant.
    [HttpGet("agentes")]
    public async Task<IActionResult> ListarAgentes()
    {
        var agentes = await _servicio.ListarAgentesAsync(TenantIdActual);
        return Ok(agentes);
    }
}
