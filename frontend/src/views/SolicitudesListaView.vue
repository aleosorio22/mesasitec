<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import type { CategoriaDto, Estado, Prioridad, ResultadoPaginado, SolicitudLista } from '../types/api'
import { listarSolicitudes } from '../api/solicitudes'
import { listarCategorias } from '../api/categorias'
import { ESTADOS, PRIORIDADES, etiquetaEstado, etiquetaPrioridad } from '../utils/constantes'
import { formatearFecha } from '../utils/fechas'

const router = useRouter()

const PAGE_SIZE = 20

const categorias = ref<CategoriaDto[]>([])
const datos = ref<ResultadoPaginado<SolicitudLista> | null>(null)
const cargando = ref(false)
const error = ref<string | null>(null)

// '' significa "sin filtro" (no viaja a la API; ver cliente HTTP).
const filtros = reactive({
  estado: '' as '' | Estado,
  prioridad: '' as '' | Prioridad,
  categoriaId: '',
  vencidas: false,
  q: '',
  page: 1,
})

const items = computed(() => datos.value?.items ?? [])
const totalPaginas = computed(() => Math.max(datos.value?.totalPaginas ?? 1, 1))

async function cargar(): Promise<void> {
  cargando.value = true
  error.value = null
  try {
    datos.value = await listarSolicitudes({
      estado: filtros.estado || undefined,
      prioridad: filtros.prioridad || undefined,
      categoriaId: filtros.categoriaId || undefined,
      vencidas: filtros.vencidas || undefined,
      q: filtros.q.trim() || undefined,
      page: filtros.page,
      pageSize: PAGE_SIZE,
    })
  } catch {
    error.value = 'No se pudieron cargar las solicitudes.'
  } finally {
    cargando.value = false
  }
}

// Cualquier cambio de filtro vuelve a la página 1 y recarga (server-side).
// Un solo debounce corto para todos: absorbe el tecleo en la búsqueda y
// evita recargas dobles cuando "limpiar" resetea varios filtros a la vez.
let debounce: ReturnType<typeof setTimeout> | undefined
watch(
  () => [filtros.estado, filtros.prioridad, filtros.categoriaId, filtros.vencidas, filtros.q],
  () => {
    filtros.page = 1
    clearTimeout(debounce)
    debounce = setTimeout(cargar, 300)
  },
)

watch(() => filtros.page, cargar)

function limpiarFiltros(): void {
  filtros.estado = ''
  filtros.prioridad = ''
  filtros.categoriaId = ''
  filtros.vencidas = false
  filtros.q = ''
}

function abrirDetalle(id: string): void {
  router.push(`/solicitudes/${id}`)
}

onMounted(() => {
  cargar()
  listarCategorias()
    .then((lista) => {
      categorias.value = lista
    })
    .catch(() => {
      // Sin categorías solo se pierde ese filtro; el listado funciona igual.
    })
})
</script>

<template>
  <div>
    <div class="encabezado">
      <h1>Solicitudes</h1>
      <RouterLink to="/solicitudes/nueva" class="boton boton-primario" data-testid="btn-nueva-solicitud">
        Nueva solicitud
      </RouterLink>
    </div>

    <div class="filtros">
      <input
        v-model="filtros.q"
        type="search"
        placeholder="Buscar por título, descripción o código"
        data-testid="filtro-busqueda"
      />

      <select v-model="filtros.estado" data-testid="filtro-estado">
        <option value="">Estado: todos</option>
        <option v-for="e in ESTADOS" :key="e" :value="e">{{ etiquetaEstado(e) }}</option>
      </select>

      <select v-model="filtros.prioridad" data-testid="filtro-prioridad">
        <option value="">Prioridad: todas</option>
        <option v-for="p in PRIORIDADES" :key="p" :value="p">{{ etiquetaPrioridad(p) }}</option>
      </select>

      <select v-model="filtros.categoriaId" data-testid="filtro-categoria">
        <option value="">Categoría: todas</option>
        <option v-for="c in categorias" :key="c.id" :value="c.id">{{ c.nombre }}</option>
      </select>

      <label class="filtro-check">
        <input v-model="filtros.vencidas" type="checkbox" data-testid="filtro-vencidas" />
        Solo vencidas
      </label>

      <button class="boton boton-secundario" data-testid="btn-limpiar-filtros" @click="limpiarFiltros">
        Limpiar filtros
      </button>
    </div>

    <p v-if="cargando" class="aviso" data-testid="listado-cargando">Cargando solicitudes…</p>

    <p v-else-if="error" class="aviso error-caja">
      {{ error }}
      <button class="boton boton-secundario" @click="cargar">Reintentar</button>
    </p>

    <p v-else-if="items.length === 0" class="aviso" data-testid="listado-vacio">
      No hay solicitudes que coincidan con los filtros.
    </p>

    <div v-else class="tabla-contenedor">
      <table class="tabla" data-testid="tabla-solicitudes">
        <thead>
          <tr>
            <th>Código</th>
            <th>Título</th>
            <th>Estado</th>
            <th>Prioridad</th>
            <th>Categoría</th>
            <th>Agente</th>
            <th>Límite SLA</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="s in items"
            :key="s.id"
            data-testid="fila-solicitud"
            :data-codigo="s.codigo"
            @click="abrirDetalle(s.id)"
          >
            <td data-testid="celda-codigo">{{ s.codigo }}</td>
            <td class="celda-titulo">{{ s.titulo }}</td>
            <td data-testid="celda-estado">
              <span class="chip" :class="`chip-estado-${s.estado}`">{{ etiquetaEstado(s.estado) }}</span>
            </td>
            <td data-testid="celda-prioridad">{{ etiquetaPrioridad(s.prioridad) }}</td>
            <td>{{ s.categoria.nombre }}</td>
            <td>{{ s.agente?.nombre ?? '—' }}</td>
            <td data-testid="celda-sla">
              {{ formatearFecha(s.fechaLimiteSla) }}
              <span v-if="s.vencida" class="chip chip-vencida" data-testid="badge-vencida">Vencida</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="datos && !cargando && !error" class="paginacion">
      <button
        class="boton boton-secundario"
        data-testid="paginacion-anterior"
        :disabled="filtros.page <= 1"
        @click="filtros.page--"
      >
        Anterior
      </button>

      <span data-testid="paginacion-info">Página {{ datos.page }} de {{ totalPaginas }} — {{ datos.total }} resultados</span>

      <button
        class="boton boton-secundario"
        data-testid="paginacion-siguiente"
        :disabled="filtros.page >= totalPaginas"
        @click="filtros.page++"
      >
        Siguiente
      </button>
    </div>
  </div>
</template>
