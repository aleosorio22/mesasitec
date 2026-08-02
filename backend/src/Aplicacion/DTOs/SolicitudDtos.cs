namespace Mesasitec.Aplicacion.DTOs;

// Objeto anidado "categoria" dentro de una solicitud.
public record CategoriaResumenDto(Guid Id, string Nombre);

// Objeto anidado "agente" (puede ser null si no hay agente asignado).
public record AgenteResumenDto(Guid Id, string Nombre);

// Un item de la lista de solicitudes (§6.2, endpoint 4).
public record SolicitudListaDto(
    Guid Id,
    string Codigo,
    string Titulo,
    string Estado,
    string Prioridad,
    CategoriaResumenDto Categoria,
    AgenteResumenDto? Agente,
    DateTime FechaCreacion,
    DateTime FechaLimiteSla,
    bool Vencida);

// La respuesta paginada completa.
public record ResultadoPaginado<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPaginas);