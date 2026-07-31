using Mesasitec.Dominio.Enums;

namespace Mesasitec.Dominio.Reglas;

// RN-04. Lógica pura: recibe "ahora" por parámetro en lugar de llamar a DateTime.UtcNow
// adentro, para poder probar el vencimiento con fechas fijas.
public static class CalculadoraSla
{
    private static readonly IReadOnlyDictionary<Prioridad, double> Factores =
        new Dictionary<Prioridad, double>
        {
            { Prioridad.Critica, 0.5  },
            { Prioridad.Alta,    0.75 },
            { Prioridad.Media,   1.0  },
            { Prioridad.Baja,    2.0  },
        };

    // fechaLimiteSla = fechaCreacion + (slaHoras × factor[prioridad]) horas
    public static DateTime CalcularFechaLimite(DateTime fechaCreacion, int slaHoras, Prioridad prioridad)
        => fechaCreacion.AddHours(slaHoras * Factores[prioridad]);

    // Vencida si el límite ya pasó Y el estado no es Resuelta/Cerrada/Cancelada.
    public static bool EstaVencida(DateTime fechaLimiteSla, Estado estado, DateTime ahora)
    {
        var esFinalOResuelta = estado is Estado.Resuelta or Estado.Cerrada or Estado.Cancelada;
        return !esFinalOResuelta && fechaLimiteSla < ahora;
    }
}