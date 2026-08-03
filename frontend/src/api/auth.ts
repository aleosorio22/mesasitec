import type { LoginResponse, UsuarioDto } from '../types/api'
import { api } from './http'

export function login(email: string, password: string): Promise<LoginResponse> {
  return api<LoginResponse>('/auth/login', { metodo: 'POST', cuerpo: { email, password } })
}

export function obtenerPerfil(): Promise<UsuarioDto> {
  return api<UsuarioDto>('/me')
}
