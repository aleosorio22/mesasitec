using Mesasitec.Dominio.Enums;
using Mesasitec.Dominio.Reglas;
using Xunit;

namespace Mesasitec.Tests;

public class MaquinaEstadosTests
{
    // --- Transiciones VÁLIDAS: recorremos toda la tabla de RN-02 ---
    [Theory]
    [InlineData(Estado.Nueva,     Accion.Asignar,  Estado.Asignada)]
    [InlineData(Estado.Nueva,     Accion.Cancelar, Estado.Cancelada)]
    [InlineData(Estado.Asignada,  Accion.Iniciar,  Estado.EnProceso)]
    [InlineData(Estado.Asignada,  Accion.Asignar,  Estado.Asignada)]   // reasignar
    [InlineData(Estado.Asignada,  Accion.Cancelar, Estado.Cancelada)]
    [InlineData(Estado.EnProceso, Accion.Resolver, Estado.Resuelta)]
    [InlineData(Estado.EnProceso, Accion.Asignar,  Estado.Asignada)]   // reasignar
    [InlineData(Estado.EnProceso, Accion.Cancelar, Estado.Cancelada)]
    [InlineData(Estado.Resuelta,  Accion.Cerrar,   Estado.Cerrada)]
    [InlineData(Estado.Resuelta,  Accion.Reabrir,  Estado.EnProceso)]
    public void TryAplicar_TransicionValida_DevuelveTrueYEstadoDestino(
        Estado actual, Accion accion, Estado esperado)
    {
        bool ok = MaquinaEstados.TryAplicar(actual, accion, out Estado destino);

        Assert.True(ok);
        Assert.Equal(esperado, destino);
    }

    // --- Transiciones INVÁLIDAS: incluye los dos estados finales ---
    [Theory]
    [InlineData(Estado.Nueva,     Accion.Resolver)]  // Nueva no admite resolver
    [InlineData(Estado.Nueva,     Accion.Iniciar)]   // ni iniciar
    [InlineData(Estado.Resuelta,  Accion.Asignar)]   // Resuelta no admite asignar
    [InlineData(Estado.Cerrada,   Accion.Reabrir)]   // Cerrada es FINAL
    [InlineData(Estado.Cancelada, Accion.Iniciar)]   // Cancelada es FINAL
    public void TryAplicar_TransicionInvalida_DevuelveFalse(Estado actual, Accion accion)
    {
        bool ok = MaquinaEstados.TryAplicar(actual, accion, out _);

        Assert.False(ok);
    }

    // --- AccionesPermitidas: lo que alimentará los botones del frontend ---
    [Fact]
    public void AccionesPermitidas_Nueva_DevuelveAsignarYCancelar()
    {
        var acciones = MaquinaEstados.AccionesPermitidas(Estado.Nueva);

        Assert.Equal(2, acciones.Count);
        Assert.Contains(Accion.Asignar, acciones);
        Assert.Contains(Accion.Cancelar, acciones);
    }

    [Fact]
    public void AccionesPermitidas_Cerrada_EstadoFinal_DevuelveVacio()
    {
        var acciones = MaquinaEstados.AccionesPermitidas(Estado.Cerrada);

        Assert.Empty(acciones);
    }
}