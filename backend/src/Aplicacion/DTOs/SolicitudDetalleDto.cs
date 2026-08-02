namespace Mesasitec.Aplicacion.DTOs;

// Objeto anidado "solicitante" (reusa la forma de resumen: id + nombre).
public record SolicitanteResumenDto(Guid Id, string Nombre);

// El objeto COMPLETO de una solicitud (§6.2, endpoint 6).
// Se usa en: POST (crear), GET /{id} (detalle), PUT (editar), transiciones.
public record SolicitudDetalleDto(
    Guid Id,
    string Codigo,
    string Titulo,
    string Descripcion,
    string Estado,
    string Prioridad,
    CategoriaResumenDto Categoria,
    SolicitanteResumenDto Solicitante,
    AgenteResumenDto? Agente,
    DateTime FechaCreacion,
    DateTime FechaLimiteSla,
    DateTime? FechaResolucion,
    string? MotivoResolucion,
    string? MotivoCancelacion,
    bool Vencida);