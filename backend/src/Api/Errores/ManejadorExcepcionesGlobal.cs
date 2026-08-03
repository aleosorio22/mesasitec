using Microsoft.AspNetCore.Diagnostics;

namespace Mesasitec.Api.Errores;

// Atrapa CUALQUIER excepción no controlada y la convierte en un 500 con el
// formato del contrato (§6.1), en vez de un stack trace crudo. §5.3 obligatorio.
public class ManejadorExcepcionesGlobal : IExceptionHandler
{
    private readonly ILogger<ManejadorExcepcionesGlobal> _logger;

    public ManejadorExcepcionesGlobal(ILogger<ManejadorExcepcionesGlobal> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Registramos la excepción real en el log del servidor (para depurar),
        // pero NO la exponemos al cliente.
        _logger.LogError(exception, "Excepción no controlada en {Ruta}", httpContext.Request.Path);

        var error = ErroresApi.Interno();

        httpContext.Response.StatusCode = error.Status;              // 500
        // El contentType va en la llamada: WriteAsJsonAsync pisa Response.ContentType.
        await httpContext.Response.WriteAsJsonAsync(
            error, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}