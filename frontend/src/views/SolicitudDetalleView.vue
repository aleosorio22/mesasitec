<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import type { Accion, AgenteResumen, SolicitudDetalle, TransicionRequest } from '../types/api'
import { ejecutarTransicion, obtenerSolicitud } from '../api/solicitudes'
import { listarAgentes } from '../api/usuarios'
import { ErrorApi } from '../api/http'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { accionesDisponibles, puedeEditar } from '../utils/reglas'
import { ETIQUETAS_ACCION, etiquetaEstado, etiquetaPrioridad } from '../utils/constantes'
import { formatearFecha } from '../utils/fechas'

const route = useRoute()
const auth = useAuthStore()
const toast = useToastStore()

const id = String(route.params.id)

const solicitud = ref<SolicitudDetalle | null>(null)
const cargando = ref(false)
const error = ref<string | null>(null)

const esDueno = computed(
  () => solicitud.value !== null && solicitud.value.solicitante.id === auth.usuario?.id,
)

// §7.5: los botones que no correspondan al estado o al rol NO se renderizan.
// La lista sale de la intersección RN-02 ∩ RN-03 (utils/reglas.ts).
const acciones = computed<Accion[]>(() => {
  if (!solicitud.value || !auth.usuario) return []
  return accionesDisponibles(solicitud.value.estado, auth.usuario.rol, esDueno.value)
})

const mostrarEditar = computed(() => {
  if (!solicitud.value || !auth.usuario) return false
  return puedeEditar(solicitud.value.estado, auth.usuario.rol, esDueno.value)
})

async function cargar(): Promise<void> {
  cargando.value = true
  error.value = null
  try {
    solicitud.value = await obtenerSolicitud(id)
  } catch (e) {
    error.value =
      e instanceof ErrorApi && e.status === 404
        ? 'La solicitud no existe.'
        : 'No se pudo cargar la solicitud.'
  } finally {
    cargando.value = false
  }
}

// ---- Modal de acción ----
const modal = reactive({
  abierto: false,
  accion: null as Accion | null,
  agenteId: '',
  motivo: '',
  error: '',
  enviando: false,
})
const agentes = ref<AgenteResumen[]>([])

function abrirModal(accion: Accion): void {
  modal.abierto = true
  modal.accion = accion
  modal.agenteId = ''
  modal.motivo = ''
  modal.error = ''
  if (accion === 'asignar' && agentes.value.length === 0) {
    listarAgentes()
      .then((lista) => {
        agentes.value = lista
      })
      .catch(() => {
        modal.error = 'No se pudo cargar la lista de agentes.'
      })
  }
}

function cerrarModal(): void {
  if (!modal.enviando) modal.abierto = false
}

async function confirmarAccion(): Promise<void> {
  if (modal.accion === null || solicitud.value === null) return

  // Validación en el cliente, espejo de RN-05 y RN-06 (el backend revalida).
  if (modal.accion === 'asignar' && modal.agenteId === '') {
    modal.error = 'Selecciona un agente.'
    return
  }
  if (modal.accion === 'resolver' && modal.motivo.trim().length < 20) {
    modal.error = 'El motivo de resolución debe tener al menos 20 caracteres.'
    return
  }
  if (modal.accion === 'cancelar' && modal.motivo.trim().length < 10) {
    modal.error = 'El motivo de cancelación debe tener al menos 10 caracteres.'
    return
  }

  const cuerpo: TransicionRequest = { accion: modal.accion }
  if (modal.accion === 'asignar') cuerpo.agenteId = modal.agenteId
  if (modal.accion === 'resolver' || modal.accion === 'cancelar') cuerpo.motivo = modal.motivo.trim()

  modal.enviando = true
  modal.error = ''
  try {
    solicitud.value = await ejecutarTransicion(solicitud.value.id, cuerpo)
    modal.abierto = false
    toast.mostrar(`Acción "${ETIQUETAS_ACCION[modal.accion]}" aplicada.`)
  } catch (e) {
    modal.error = e instanceof ErrorApi ? e.message : 'No se pudo aplicar la acción.'
  } finally {
    modal.enviando = false
  }
}

onMounted(cargar)
</script>

<template>
  <div>
    <RouterLink to="/solicitudes" class="enlace-volver">← Volver al listado</RouterLink>

    <p v-if="cargando" class="aviso">Cargando solicitud…</p>

    <p v-else-if="error" class="aviso error-caja">
      {{ error }}
      <button class="boton boton-secundario" @click="cargar">Reintentar</button>
    </p>

    <div v-else-if="solicitud" class="detalle">
      <div class="encabezado">
        <div>
          <p class="detalle-codigo" data-testid="detalle-codigo">{{ solicitud.codigo }}</p>
          <h1 data-testid="detalle-titulo">{{ solicitud.titulo }}</h1>
        </div>
        <div class="detalle-acciones">
          <RouterLink
            v-if="mostrarEditar"
            :to="`/solicitudes/${solicitud.id}/editar`"
            class="boton boton-secundario"
            data-testid="btn-editar"
          >
            Editar
          </RouterLink>
          <button
            v-for="accion in acciones"
            :key="accion"
            class="boton boton-primario"
            :data-testid="`btn-accion-${accion}`"
            @click="abrirModal(accion)"
          >
            {{ ETIQUETAS_ACCION[accion] }}
          </button>
        </div>
      </div>

      <p class="detalle-descripcion" data-testid="detalle-descripcion">{{ solicitud.descripcion }}</p>

      <dl class="detalle-campos">
        <div>
          <dt>Estado</dt>
          <dd data-testid="detalle-estado">
            <span class="chip" :class="`chip-estado-${solicitud.estado}`">{{ etiquetaEstado(solicitud.estado) }}</span>
          </dd>
        </div>
        <div>
          <dt>Prioridad</dt>
          <dd data-testid="detalle-prioridad">{{ etiquetaPrioridad(solicitud.prioridad) }}</dd>
        </div>
        <div>
          <dt>Categoría</dt>
          <dd data-testid="detalle-categoria">{{ solicitud.categoria.nombre }}</dd>
        </div>
        <div>
          <dt>Solicitante</dt>
          <dd>{{ solicitud.solicitante.nombre }}</dd>
        </div>
        <div>
          <dt>Agente</dt>
          <dd data-testid="detalle-agente">{{ solicitud.agente?.nombre ?? 'Sin asignar' }}</dd>
        </div>
        <div>
          <dt>Creada</dt>
          <dd data-testid="detalle-fecha-creacion">{{ formatearFecha(solicitud.fechaCreacion) }}</dd>
        </div>
        <div>
          <dt>Límite SLA</dt>
          <dd data-testid="detalle-fecha-limite">
            {{ formatearFecha(solicitud.fechaLimiteSla) }}
            <span v-if="solicitud.vencida" class="chip chip-vencida" data-testid="detalle-vencida">Vencida</span>
          </dd>
        </div>
        <div v-if="solicitud.fechaResolucion">
          <dt>Resuelta</dt>
          <dd>{{ formatearFecha(solicitud.fechaResolucion) }}</dd>
        </div>
        <div v-if="solicitud.motivoResolucion || solicitud.motivoCancelacion" class="detalle-motivo">
          <dt>{{ solicitud.motivoCancelacion ? 'Motivo de cancelación' : 'Motivo de resolución' }}</dt>
          <dd data-testid="detalle-motivo">{{ solicitud.motivoCancelacion ?? solicitud.motivoResolucion }}</dd>
        </div>
      </dl>
    </div>

    <!-- Modal de confirmación de acción -->
    <div v-if="modal.abierto && modal.accion !== null" class="modal-fondo" @click.self="cerrarModal">
      <div class="modal" data-testid="modal-accion">
        <h2>{{ ETIQUETAS_ACCION[modal.accion] }}</h2>

        <label v-if="modal.accion === 'asignar'">
          Agente
          <select v-model="modal.agenteId" data-testid="modal-select-agente">
            <option value="" disabled>Selecciona un agente</option>
            <option v-for="agente in agentes" :key="agente.id" :value="agente.id">{{ agente.nombre }}</option>
          </select>
        </label>

        <label v-else-if="modal.accion === 'resolver' || modal.accion === 'cancelar'">
          Motivo
          <textarea
            v-model="modal.motivo"
            rows="4"
            :placeholder="
              modal.accion === 'resolver'
                ? 'Describe cómo se resolvió (mínimo 20 caracteres)'
                : 'Explica por qué se cancela (mínimo 10 caracteres)'
            "
            data-testid="modal-motivo"
          ></textarea>
        </label>

        <p v-else>¿Confirmas la acción "{{ ETIQUETAS_ACCION[modal.accion] }}" sobre esta solicitud?</p>

        <p v-if="modal.error" class="error-texto" data-testid="modal-error">{{ modal.error }}</p>

        <div class="modal-botones">
          <button
            class="boton boton-secundario"
            data-testid="modal-cancelar"
            :disabled="modal.enviando"
            @click="cerrarModal"
          >
            Cancelar
          </button>
          <button
            class="boton boton-primario"
            data-testid="modal-confirmar"
            :disabled="modal.enviando"
            @click="confirmarAccion"
          >
            {{ modal.enviando ? 'Aplicando…' : 'Confirmar' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
