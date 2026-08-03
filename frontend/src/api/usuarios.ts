import type { AgenteResumen } from '../types/api'
import { api } from './http'

// Endpoint extra al contrato (declarado en DECISIONES.md): agentes y admins
// activos del tenant, para poblar el select del modal de asignación.
export function listarAgentes(): Promise<AgenteResumen[]> {
  return api<AgenteResumen[]>('/usuarios/agentes')
}
