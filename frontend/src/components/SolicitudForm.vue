<script setup lang="ts">
// Formulario de solicitud, reutilizado por /solicitudes/nueva y
// /solicitudes/:id/editar (§7.3). El modo lo decide la vista que lo monta.
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import type { CategoriaDto, Prioridad, SolicitudDatos } from '../types/api'
import { crearSolicitud, editarSolicitud, obtenerSolicitud } from '../api/solicitudes'
import { listarCategorias } from '../api/categorias'
import { ErrorApi } from '../api/http'
import { useToastStore } from '../stores/toast'
import { PRIORIDADES, etiquetaPrioridad } from '../utils/constantes'

const props = defineProps<{
  modo: 'crear' | 'editar'
  solicitudId?: string
}>()

const router = useRouter()
const toast = useToastStore()

const campos = reactive({
  titulo: '',
  descripcion: '',
  categoriaId: '',
  prioridad: 'Media' as Prioridad,
})

// Errores de validación por campo (cliente y servidor caen aquí).
const errores = reactive({ titulo: '', descripcion: '', categoria: '' })

const categorias = ref<CategoriaDto[]>([])
const cargando = ref(false)
const errorCarga = ref<string | null>(null)
const enviando = ref(false)

// Misma validación que el backend (§3): título 5–120, descripción 10–4000.
function validar(): boolean {
  const titulo = campos.titulo.trim()
  const descripcion = campos.descripcion.trim()

  errores.titulo =
    titulo.length < 5 || titulo.length > 120 ? 'El título debe tener entre 5 y 120 caracteres.' : ''
  errores.descripcion =
    descripcion.length < 10 || descripcion.length > 4000
      ? 'La descripción debe tener entre 10 y 4000 caracteres.'
      : ''
  errores.categoria = campos.categoriaId === '' ? 'Selecciona una categoría.' : ''

  return errores.titulo === '' && errores.descripcion === '' && errores.categoria === ''
}

async function guardar(): Promise<void> {
  if (!validar()) return

  const datos: SolicitudDatos = {
    titulo: campos.titulo.trim(),
    descripcion: campos.descripcion.trim(),
    categoriaId: campos.categoriaId,
    prioridad: campos.prioridad,
  }

  enviando.value = true
  try {
    const guardada =
      props.modo === 'crear'
        ? await crearSolicitud(datos)
        : await editarSolicitud(props.solicitudId ?? '', datos)

    toast.mostrar(
      props.modo === 'crear' ? `Solicitud ${guardada.codigo} creada.` : 'Solicitud actualizada.',
    )
    router.push(`/solicitudes/${guardada.id}`)
  } catch (e) {
    if (e instanceof ErrorApi && e.errores) {
      // 422 VALIDACION: el backend manda los errores por campo; se muestran
      // en su lugar (por si algo se escapó de la validación del cliente).
      errores.titulo = e.errores.titulo?.[0] ?? ''
      errores.descripcion = e.errores.descripcion?.[0] ?? ''
      errores.categoria = e.errores.categoriaId?.[0] ?? ''
      if (!errores.titulo && !errores.descripcion && !errores.categoria) {
        toast.mostrar(e.message, 'error')
      }
    } else {
      toast.mostrar(e instanceof ErrorApi ? e.message : 'No se pudo guardar la solicitud.', 'error')
    }
  } finally {
    enviando.value = false
  }
}

function cancelar(): void {
  if (props.modo === 'editar' && props.solicitudId !== undefined) {
    router.push(`/solicitudes/${props.solicitudId}`)
  } else {
    router.push('/solicitudes')
  }
}

onMounted(async () => {
  cargando.value = true
  errorCarga.value = null
  try {
    categorias.value = await listarCategorias()

    if (props.modo === 'editar' && props.solicitudId !== undefined) {
      const solicitud = await obtenerSolicitud(props.solicitudId)
      campos.titulo = solicitud.titulo
      campos.descripcion = solicitud.descripcion
      campos.categoriaId = solicitud.categoria.id
      campos.prioridad = solicitud.prioridad
    }
  } catch (e) {
    errorCarga.value =
      e instanceof ErrorApi && e.status === 404
        ? 'La solicitud no existe.'
        : 'No se pudo cargar el formulario.'
  } finally {
    cargando.value = false
  }
})
</script>

<template>
  <div class="form-caja">
    <h1>{{ props.modo === 'crear' ? 'Nueva solicitud' : 'Editar solicitud' }}</h1>

    <p v-if="cargando" class="aviso">Cargando…</p>

    <p v-else-if="errorCarga" class="aviso error-caja">{{ errorCarga }}</p>

    <form v-else @submit.prevent="guardar">
      <label>
        Título
        <input v-model="campos.titulo" type="text" data-testid="form-titulo" />
        <span v-if="errores.titulo" class="error-texto" data-testid="error-titulo">{{ errores.titulo }}</span>
      </label>

      <label>
        Descripción
        <textarea v-model="campos.descripcion" rows="6" data-testid="form-descripcion"></textarea>
        <span v-if="errores.descripcion" class="error-texto" data-testid="error-descripcion">
          {{ errores.descripcion }}
        </span>
      </label>

      <label>
        Categoría
        <select v-model="campos.categoriaId" data-testid="form-categoria">
          <option value="" disabled>Selecciona una categoría</option>
          <option v-for="c in categorias" :key="c.id" :value="c.id">
            {{ c.nombre }} (SLA {{ c.slaHoras }} h)
          </option>
        </select>
        <span v-if="errores.categoria" class="error-texto" data-testid="error-categoria">
          {{ errores.categoria }}
        </span>
      </label>

      <label>
        Prioridad
        <select v-model="campos.prioridad" data-testid="form-prioridad">
          <option v-for="p in PRIORIDADES" :key="p" :value="p">{{ etiquetaPrioridad(p) }}</option>
        </select>
      </label>

      <div class="form-botones">
        <button
          class="boton boton-secundario"
          type="button"
          data-testid="form-cancelar"
          :disabled="enviando"
          @click="cancelar"
        >
          Cancelar
        </button>
        <button class="boton boton-primario" type="submit" :disabled="enviando" data-testid="form-submit">
          {{ enviando ? 'Guardando…' : props.modo === 'crear' ? 'Crear solicitud' : 'Guardar cambios' }}
        </button>
      </div>
    </form>
  </div>
</template>
