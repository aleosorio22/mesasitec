using Mesasitec.Dominio.Enums;

namespace Mesasitec.Dominio.Reglas;

// RN-03: qué rol puede ejecutar cada acción del flujo.
public static class PermisosAcciones
{
    // ¿Puede este rol ejecutar esta acción?
    // 'esDueno' indica si el usuario es el solicitante de la solicitud
    // (relevante para 'cerrar', que un Solicitante solo puede sobre las propias).
    public static bool PuedeEjecutar(Accion accion, Rol rol, bool esDueno)
        => accion switch
        {
            // asignar/iniciar/resolver/reabrir: Admin o Agente.
            Accion.Asignar or Accion.Iniciar or Accion.Resolver or Accion.Reabrir
                => rol is Rol.Admin or Rol.Agente,

            // cerrar: Admin, Agente, o Solicitante (solo las propias).
            Accion.Cerrar
                => rol is Rol.Admin or Rol.Agente || (rol == Rol.Solicitante && esDueno),

            // cancelar: SOLO Admin.
            Accion.Cancelar
                => rol == Rol.Admin,

            _ => false,
        };
}