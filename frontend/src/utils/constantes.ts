import type { Accion, Estado, Prioridad } from '../types/api'

export const ESTADOS: Estado[] = ['Nueva', 'Asignada', 'EnProceso', 'Resuelta', 'Cerrada', 'Cancelada']

export const PRIORIDADES: Prioridad[] = ['Baja', 'Media', 'Alta', 'Critica']

// Etiquetas para mostrar. Los VALORES que viajan a la API son los del tipo.
export function etiquetaEstado(estado: Estado): string {
  return estado === 'EnProceso' ? 'En proceso' : estado
}

export function etiquetaPrioridad(prioridad: Prioridad): string {
  return prioridad === 'Critica' ? 'Crítica' : prioridad
}

export const ETIQUETAS_ACCION: Record<Accion, string> = {
  asignar: 'Asignar',
  iniciar: 'Iniciar',
  resolver: 'Resolver',
  cerrar: 'Cerrar',
  reabrir: 'Reabrir',
  cancelar: 'Cancelar',
}
