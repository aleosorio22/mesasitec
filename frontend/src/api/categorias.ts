import type { CategoriaDto } from '../types/api'
import { api } from './http'

export function listarCategorias(): Promise<CategoriaDto[]> {
  return api<CategoriaDto[]>('/categorias')
}
