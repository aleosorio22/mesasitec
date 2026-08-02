using Mesasitec.Dominio.Enums;

namespace Mesasitec.Aplicacion.DTOs;

// Agrupa todos los parámetros de consulta de GET /solicitudes.
// Todo nullable: si un filtro llega null, no se aplica.
public class SolicitudFiltros
{
    public Estado? Estado { get; set; }
    public Prioridad? Prioridad { get; set; }
    public Guid? CategoriaId { get; set; }
    public Guid? AgenteId { get; set; }
    public bool? Vencidas { get; set; }
    public string? Q { get; set; }

    // Paginación y orden (los usaremos en los siguientes pasos; los declaro ya).
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}