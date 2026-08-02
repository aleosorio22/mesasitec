using System.ComponentModel.DataAnnotations;
using Mesasitec.Dominio.Enums;

namespace Mesasitec.Aplicacion.DTOs;

// Lo que el cliente manda al crear una solicitud (§6.2, endpoint 5).
// Es una clase (no record) para que la validación por atributos funcione sin fricción.
public class CrearSolicitudRequest
{
    [Required]
    [StringLength(120, MinimumLength = 5,
        ErrorMessage = "El título debe tener entre 5 y 120 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [StringLength(4000, MinimumLength = 10,
        ErrorMessage = "La descripción debe tener entre 10 y 4000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    public Guid CategoriaId { get; set; }

    public Prioridad Prioridad { get; set; }
}