// Persistencia de la sesión en localStorage.
// Módulo aparte para que tanto el cliente HTTP como el store de auth puedan
// usarlo sin importarse entre ellos (evita dependencias circulares).
import type { UsuarioDto } from '../types/api'

const TOKEN_KEY = 'mesasitec.token'
const USUARIO_KEY = 'mesasitec.usuario'

export function obtenerToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function obtenerUsuario(): UsuarioDto | null {
  const crudo = localStorage.getItem(USUARIO_KEY)
  if (crudo === null) return null
  try {
    return JSON.parse(crudo) as UsuarioDto
  } catch {
    return null
  }
}

export function guardarSesion(token: string, usuario: UsuarioDto): void {
  localStorage.setItem(TOKEN_KEY, token)
  localStorage.setItem(USUARIO_KEY, JSON.stringify(usuario))
}

export function limpiarSesion(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USUARIO_KEY)
}
