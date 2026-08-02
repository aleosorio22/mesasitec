using Mesasitec.Aplicacion.DTOs;

namespace Mesasitec.Aplicacion.Contratos;

public interface IServicioCategorias
{
    // Categorías activas del tenant (RN-01 + solo activas).
    Task<IReadOnlyList<CategoriaDto>> ListarActivasAsync(Guid tenantId);
}