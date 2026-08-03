using System.Text.Json.Serialization;

namespace Mesasitec.Api.Errores;

// Forma única de los errores de la API (§6.1). application/problem+json.
public record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Codigo,
    // Solo en errores de validación (400/422). Null en el resto 
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errores = null);