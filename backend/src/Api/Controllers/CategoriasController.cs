using Mesasitec.Aplicacion.Contratos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mesasitec.Api.Controllers;

[Route("api/v1/categorias")]
[Authorize]
public class CategoriasController : ApiControllerBase
{
    private readonly IServicioCategorias _servicio;

    public CategoriasController(IServicioCategorias servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var categorias = await _servicio.ListarActivasAsync(TenantIdActual);
        return Ok(categorias);
    }
}