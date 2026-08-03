// Cliente HTTP único de la aplicación (§7.1): TODAS las llamadas a la API
// pasan por aquí. Inyecta el token en cada petición y ante un 401 (sesión
// vencida o token inválido) limpia la sesión y redirige a /login.
import type { ProblemaApi } from '../types/api'
import { limpiarSesion, obtenerToken } from './sesion'

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5080/api/v1'

// Error tipado con el problema que devolvió la API (§6.1).
// `message` es el `detail`, pensado para mostrarse tal cual al usuario.
export class ErrorApi extends Error {
  readonly problema: ProblemaApi

  constructor(problema: ProblemaApi) {
    super(problema.detail)
    this.problema = problema
  }

  get status(): number {
    return this.problema.status
  }

  get codigo(): string {
    return this.problema.codigo
  }

  get errores(): Record<string, string[]> | undefined {
    return this.problema.errores
  }
}

type ValorQuery = string | number | boolean | undefined

interface Opciones {
  metodo?: 'GET' | 'POST' | 'PUT'
  cuerpo?: unknown
  query?: Record<string, ValorQuery>
}

export async function api<T>(ruta: string, opciones: Opciones = {}): Promise<T> {
  const url = new URL(BASE_URL + ruta)
  if (opciones.query) {
    for (const [clave, valor] of Object.entries(opciones.query)) {
      // Los filtros vacíos no viajan: la API interpreta ausencia como "sin filtro".
      if (valor !== undefined && valor !== '') url.searchParams.set(clave, String(valor))
    }
  }

  const headers: Record<string, string> = {}
  const token = obtenerToken()
  if (token !== null) headers.Authorization = `Bearer ${token}`
  if (opciones.cuerpo !== undefined) headers['Content-Type'] = 'application/json'

  const respuesta = await fetch(url, {
    method: opciones.metodo ?? 'GET',
    headers,
    body: opciones.cuerpo !== undefined ? JSON.stringify(opciones.cuerpo) : undefined,
  })

  if (respuesta.ok) {
    return (await respuesta.json()) as T
  }

  // 401 fuera del login = la sesión ya no sirve. En el login es solo
  // "credenciales incorrectas" y lo maneja la vista.
  if (respuesta.status === 401 && !ruta.startsWith('/auth/login')) {
    limpiarSesion()
    window.location.href = '/login'
  }

  let problema: ProblemaApi
  try {
    problema = (await respuesta.json()) as ProblemaApi
  } catch {
    // Por si algo respondió sin cuerpo problem+json (no debería pasar).
    problema = {
      type: 'about:blank',
      title: 'Error',
      status: respuesta.status,
      detail: 'Ocurrió un error inesperado.',
      codigo: 'ERROR_DESCONOCIDO',
    }
  }
  throw new ErrorApi(problema)
}
