namespace Mesasitec.Dominio.Entidades;

public class Categoria
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int SlaHoras { get; set; }
    public bool Activo { get; set; } = true;
}