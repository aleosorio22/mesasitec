// DTOs del contrato de la API (§6.2), escritos a mano en camelCase.
// Los enums del dominio van como union types de literales: el backend los
// serializa por nombre ("Nueva", "Critica"...), así que esto ES el contrato.

export type Rol = 'Admin' | 'Agente' | 'Solicitante'

export type Estado = 'Nueva' | 'Asignada' | 'EnProceso' | 'Resuelta' | 'Cerrada' | 'Cancelada'

export type Prioridad = 'Baja' | 'Media' | 'Alta' | 'Critica'

// Las acciones viajan en minúscula en el cuerpo de la petición (§6.2, endpoint 8).
export type Accion = 'asignar' | 'iniciar' | 'resolver' | 'cerrar' | 'reabrir' | 'cancelar'

export interface UsuarioDto {
  id: string
  nombre: string
  email: string
  rol: Rol
  tenantId: string
  tenantNombre: string
}

export interface LoginResponse {
  accessToken: string
  expiraEn: number
  usuario: UsuarioDto
}

export interface CategoriaDto {
  id: string
  nombre: string
  slaHoras: number
}

export interface CategoriaResumen {
  id: string
  nombre: string
}

export interface AgenteResumen {
  id: string
  nombre: string
}

export interface SolicitanteResumen {
  id: string
  nombre: string
}

// Elemento del listado (GET /solicitudes).
export interface SolicitudLista {
  id: string
  codigo: string
  titulo: string
  estado: Estado
  prioridad: Prioridad
  categoria: CategoriaResumen
  agente: AgenteResumen | null
  fechaCreacion: string
  fechaLimiteSla: string
  vencida: boolean
}

// Objeto completo (GET /solicitudes/{id}, y respuesta de crear/editar/transiciones).
export interface SolicitudDetalle extends SolicitudLista {
  descripcion: string
  solicitante: SolicitanteResumen
  fechaResolucion: string | null
  motivoResolucion: string | null
  motivoCancelacion: string | null
}

export interface ResultadoPaginado<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
  totalPaginas: number
}

// Cuerpo de POST y PUT /solicitudes (mismos campos en crear y editar).
export interface SolicitudDatos {
  titulo: string
  descripcion: string
  categoriaId: string
  prioridad: Prioridad
}

export interface TransicionRequest {
  accion: Accion
  agenteId?: string
  motivo?: string
}

// Formato de error del contrato (§6.1): application/problem+json + codigo.
export interface ProblemaApi {
  type: string
  title: string
  status: number
  detail: string
  codigo: string
  errores?: Record<string, string[]>
}
