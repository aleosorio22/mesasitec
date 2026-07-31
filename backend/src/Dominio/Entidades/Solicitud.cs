using Mesasitec.Dominio.Enums;

namespace Mesasitec.Dominio.Entidades;

public class Solicitud
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Codigo { get; set; } = string.Empty;       
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public Prioridad Prioridad { get; set; }
    public Estado Estado { get; set; } = Estado.Nueva;
    public Guid SolicitanteId { get; set; }
    public Guid? AgenteId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaLimiteSla { get; set; }       
    public DateTime? FechaResolucion { get; set; }
    public string? MotivoResolucion { get; set; }
    public string? MotivoCancelacion { get; set; }

    // Navegaciones 
    public Categoria? Categoria { get; set; }
    public Usuario? Solicitante { get; set; }
    public Usuario? Agente { get; set; }
}