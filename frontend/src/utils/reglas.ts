// Espejo en el cliente de RN-02 (máquina de estados) y RN-03 (permisos por rol).
// La fuente de verdad sigue siendo el backend; esto existe porque §7.5 exige
// que los botones de acciones no permitidas NO se rendericen en el DOM.
import type { Accion, Estado, Rol } from '../types/api'

// RN-02: acciones que admite cada estado.
const TRANSICIONES: Record<Estado, Accion[]> = {
  Nueva: ['asignar', 'cancelar'],
  Asignada: ['iniciar', 'asignar', 'cancelar'],
  EnProceso: ['resolver', 'asignar', 'cancelar'],
  Resuelta: ['cerrar', 'reabrir'],
  Cerrada: [],
  Cancelada: [],
}

// RN-03: qué rol puede ejecutar cada acción.
function rolPermite(accion: Accion, rol: Rol, esDueno: boolean): boolean {
  switch (accion) {
    case 'asignar':
    case 'iniciar':
    case 'resolver':
    case 'reabrir':
      return rol === 'Admin' || rol === 'Agente'
    case 'cerrar':
      return rol === 'Admin' || rol === 'Agente' || (rol === 'Solicitante' && esDueno)
    case 'cancelar':
      return rol === 'Admin'
  }
}

// Intersección de RN-02 y RN-03: las acciones que este usuario puede ejecutar
// sobre una solicitud en este estado. Esto decide qué botones existen.
export function accionesDisponibles(estado: Estado, rol: Rol, esDueno: boolean): Accion[] {
  return TRANSICIONES[estado].filter((accion) => rolPermite(accion, rol, esDueno))
}

// RN-03 para editar: Solicitante solo las propias y solo en Nueva;
// Admin/Agente mientras no esté resuelta ni en estado final
// (misma decisión que aplica el backend, documentada en DECISIONES.md).
export function puedeEditar(estado: Estado, rol: Rol, esDueno: boolean): boolean {
  if (rol === 'Solicitante') return esDueno && estado === 'Nueva'
  return estado === 'Nueva' || estado === 'Asignada' || estado === 'EnProceso'
}
