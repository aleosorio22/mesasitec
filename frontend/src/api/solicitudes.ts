import type {
  Estado,
  Prioridad,
  ResultadoPaginado,
  SolicitudDatos,
  SolicitudDetalle,
  SolicitudLista,
  TransicionRequest,
} from '../types/api'
import { api } from './http'

// Parámetros de consulta de GET /solicitudes (§6.2). Los vacíos no viajan.
export interface FiltrosListado {
  estado?: Estado
  prioridad?: Prioridad
  categoriaId?: string
  q?: string
  vencidas?: boolean
  page: number
  pageSize: number
  sort?: string
}

export function listarSolicitudes(filtros: FiltrosListado): Promise<ResultadoPaginado<SolicitudLista>> {
  return api<ResultadoPaginado<SolicitudLista>>('/solicitudes', { query: { ...filtros } })
}

export function obtenerSolicitud(id: string): Promise<SolicitudDetalle> {
  return api<SolicitudDetalle>(`/solicitudes/${id}`)
}

export function crearSolicitud(datos: SolicitudDatos): Promise<SolicitudDetalle> {
  return api<SolicitudDetalle>('/solicitudes', { metodo: 'POST', cuerpo: datos })
}

export function editarSolicitud(id: string, datos: SolicitudDatos): Promise<SolicitudDetalle> {
  return api<SolicitudDetalle>(`/solicitudes/${id}`, { metodo: 'PUT', cuerpo: datos })
}

export function ejecutarTransicion(id: string, datos: TransicionRequest): Promise<SolicitudDetalle> {
  return api<SolicitudDetalle>(`/solicitudes/${id}/transiciones`, { metodo: 'POST', cuerpo: datos })
}

// Re-export de tipos que las vistas usan junto con estas funciones.
export type { Prioridad, SolicitudDatos }
