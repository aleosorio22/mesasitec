namespace Mesasitec.Aplicacion.DTOs;

// Categoría tal como la devuelve GET /categorias (§6.2).
public record CategoriaDto(Guid Id, string Nombre, int SlaHoras);