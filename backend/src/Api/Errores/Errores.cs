namespace Mesasitec.Api.Errores;

// Fábrica centralizada de errores del contrato (§6.1).
// Un solo lugar que conoce todos los códigos y su forma.
public static class ErroresApi
{
    private const string BaseType = "https://mesasitec.local/errores/";

    public static ErrorResponse NoAutenticado(string detail = "No autenticado.") =>
        new(BaseType + "no-autenticado", "No autenticado", 401, detail, "NO_AUTENTICADO");

    public static ErrorResponse OperacionNoPermitida(string detail = "Su rol no permite esta operación.") =>
        new(BaseType + "operacion-no-permitida", "Operación no permitida", 403, detail, "OPERACION_NO_PERMITIDA");

    public static ErrorResponse RecursoNoEncontrado(string detail = "El recurso no existe.") =>
        new(BaseType + "recurso-no-encontrado", "Recurso no encontrado", 404, detail, "RECURSO_NO_ENCONTRADO");

    public static ErrorResponse TransicionInvalida(string detail) =>
        new(BaseType + "transicion-invalida", "Transición inválida", 409, detail, "TRANSICION_INVALIDA");

    public static ErrorResponse AgenteInvalido(string detail = "El agente no es válido.") =>
        new(BaseType + "agente-invalido", "Agente inválido", 422, detail, "AGENTE_INVALIDO");

    public static ErrorResponse MotivoRequerido(string detail) =>
        new(BaseType + "motivo-requerido", "Motivo requerido", 422, detail, "MOTIVO_REQUERIDO");

    public static ErrorResponse ParametroInvalido(string detail) =>
        new(BaseType + "parametro-invalido", "Parámetro inválido", 400, detail, "PARAMETRO_INVALIDO");

    public static ErrorResponse Validacion(string detail, IReadOnlyDictionary<string, string[]>? errores = null) =>
        new(BaseType + "validacion", "Error de validación", 422, detail, "VALIDACION", errores);

    // Para lo inesperado (500). No expone detalles internos.
    public static ErrorResponse Interno() =>
        new(BaseType + "interno", "Error interno", 500, "Ocurrió un error inesperado.", "ERROR_INTERNO");
}