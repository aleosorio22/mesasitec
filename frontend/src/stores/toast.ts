import { ref } from 'vue'
import { defineStore } from 'pinia'

// Mensajes flotantes de confirmación o error. Uno a la vez, se autodescartan.
export const useToastStore = defineStore('toast', () => {
  const mensaje = ref<string | null>(null)
  const tipo = ref<'exito' | 'error'>('exito')
  let temporizador: ReturnType<typeof setTimeout> | undefined

  function mostrar(texto: string, tipoMensaje: 'exito' | 'error' = 'exito'): void {
    mensaje.value = texto
    tipo.value = tipoMensaje
    clearTimeout(temporizador)
    temporizador = setTimeout(() => {
      mensaje.value = null
    }, 4000)
  }

  return { mensaje, tipo, mostrar }
})
