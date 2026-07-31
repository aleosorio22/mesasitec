using Mesasitec.Dominio.Enums;
using Mesasitec.Dominio.Reglas;
using Xunit;

namespace Mesasitec.Tests;

public class CalculadoraSlaTests
{
    // Fecha base fija para todas las pruebas de esta clase.
    // Usar una fecha "clavada" (no DateTime.UtcNow) hace que el test
    // dé el mismo resultado sin importar cuándo lo corras.
    private static readonly DateTime Creacion =
        new(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    // --- Cálculo del límite: los 4 factores de prioridad de RN-04 ---
    // Incluye los DOS ejemplos exactos que da el enunciado.
    [Theory]
    [InlineData(8,  Prioridad.Critica, 4)]    // ejemplo enunciado: Incidente crítica -> 4h  (8 × 0.5)
    [InlineData(24, Prioridad.Baja,    48)]   // ejemplo enunciado: Consulta baja    -> 48h (24 × 2.0)
    [InlineData(8,  Prioridad.Alta,    6)]    // 8 × 0.75
    [InlineData(40, Prioridad.Media,   40)]   // 40 × 1.0
    public void CalcularFechaLimite_AplicaFactorSegunPrioridad(
        int slaHoras, Prioridad prioridad, int horasEsperadas)
    {
        DateTime limite = CalculadoraSla.CalcularFechaLimite(Creacion, slaHoras, prioridad);

        Assert.Equal(Creacion.AddHours(horasEsperadas), limite);
    }

    // --- Vencida: el límite ya pasó Y el estado sigue "vivo" ---
    [Fact]
    public void EstaVencida_LimitePasadoYEstadoEnProceso_EsVencida()
    {
        DateTime limite = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        DateTime ahora  = new(2026, 1, 15, 13, 0, 0, DateTimeKind.Utc); // 1h después del límite

        Assert.True(CalculadoraSla.EstaVencida(limite, Estado.EnProceso, ahora));
    }

    // --- NO vencida: el límite pasó, pero el estado ya está cerrado/resuelto/cancelado ---
    [Theory]
    [InlineData(Estado.Resuelta)]
    [InlineData(Estado.Cerrada)]
    [InlineData(Estado.Cancelada)]
    public void EstaVencida_EstadoFinalOResuelta_NoEsVencida(Estado estado)
    {
        DateTime limite = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        DateTime ahora  = new(2026, 1, 15, 13, 0, 0, DateTimeKind.Utc);

        Assert.False(CalculadoraSla.EstaVencida(limite, estado, ahora));
    }

    // --- NO vencida: el estado está vivo, pero el límite todavía no llega ---
    [Fact]
    public void EstaVencida_LimiteEnElFuturo_NoEsVencida()
    {
        DateTime limite = new(2026, 1, 15, 14, 0, 0, DateTimeKind.Utc);
        DateTime ahora  = new(2026, 1, 15, 13, 0, 0, DateTimeKind.Utc); // 1h ANTES del límite

        Assert.False(CalculadoraSla.EstaVencida(limite, Estado.EnProceso, ahora));
    }
}