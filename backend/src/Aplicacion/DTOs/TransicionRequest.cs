using System.ComponentModel.DataAnnotations;
using Mesasitec.Dominio.Enums;

namespace Mesasitec.Aplicacion.DTOs;

// Body de POST /solicitudes/{id}/transiciones (§6.2, endpoint 8).
// class por la validación/binding, igual que crear y editar.
public class TransicionRequest
{
    // La acción a ejecutar (asignar, iniciar, resolver, cerrar, reabrir, cancelar).
    [Required]
    public Accion Accion { get; set; }

    // Solo para 'asignar': a qué agente. Nullable (las demás acciones no lo usan).
    public Guid? AgenteId { get; set; }

    // Para 'resolver' (>=20 chars) y 'cancelar' (>=10 chars). Nullable.
    public string? Motivo { get; set; }
}