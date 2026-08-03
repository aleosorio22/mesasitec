using Mesasitec.Dominio.Enums;
using Mesasitec.Dominio.Reglas;
using Xunit;

namespace Mesasitec.Tests;

public class PermisosAccionesTests
{
    // --- Acciones que solo Admin/Agente pueden (asignar, iniciar, resolver, reabrir) ---
    [Theory]
    [InlineData(Accion.Asignar,  Rol.Admin,  true)]
    [InlineData(Accion.Asignar,  Rol.Agente, true)]
    [InlineData(Accion.Iniciar,  Rol.Admin,  true)]
    [InlineData(Accion.Iniciar,  Rol.Agente, true)]
    [InlineData(Accion.Resolver, Rol.Admin,  true)]
    [InlineData(Accion.Resolver, Rol.Agente, true)]
    [InlineData(Accion.Reabrir,  Rol.Admin,  true)]
    [InlineData(Accion.Reabrir,  Rol.Agente, true)]
    public void AdminYAgente_PuedenGestionar(Accion accion, Rol rol, bool esperado)
    {
        // esDueno no importa para estas acciones; probamos con false.
        Assert.Equal(esperado, PermisosAcciones.PuedeEjecutar(accion, rol, esDueno: false));
    }

    // --- Un Solicitante NO puede esas acciones de gestión ---
    [Theory]
    [InlineData(Accion.Asignar)]
    [InlineData(Accion.Iniciar)]
    [InlineData(Accion.Resolver)]
    [InlineData(Accion.Reabrir)]
    public void Solicitante_NoPuedeGestionar(Accion accion)
    {
        Assert.False(PermisosAcciones.PuedeEjecutar(accion, Rol.Solicitante, esDueno: true));
    }

    // --- Cancelar: SOLO Admin ---
    [Theory]
    [InlineData(Rol.Admin,       true)]
    [InlineData(Rol.Agente,      false)]   // ni siquiera el Agente
    [InlineData(Rol.Solicitante, false)]
    public void Cancelar_SoloAdmin(Rol rol, bool esperado)
    {
        Assert.Equal(esperado, PermisosAcciones.PuedeEjecutar(Accion.Cancelar, rol, esDueno: true));
    }

    // --- Cerrar: Admin y Agente siempre; Solicitante solo si es dueño ---
    [Theory]
    [InlineData(Rol.Admin,       false, true)]   // admin puede aunque no sea dueño
    [InlineData(Rol.Agente,      false, true)]   // agente igual
    [InlineData(Rol.Solicitante, true,  true)]   // solicitante dueño: SÍ
    [InlineData(Rol.Solicitante, false, false)]  // solicitante NO dueño: NO
    public void Cerrar_SolicitanteSoloSiEsDueno(Rol rol, bool esDueno, bool esperado)
    {
        Assert.Equal(esperado, PermisosAcciones.PuedeEjecutar(Accion.Cerrar, rol, esDueno));
    }
}