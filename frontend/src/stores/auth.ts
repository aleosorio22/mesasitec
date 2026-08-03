import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import type { UsuarioDto } from '../types/api'
import { login } from '../api/auth'
import { guardarSesion, limpiarSesion, obtenerToken, obtenerUsuario } from '../api/sesion'

// Sesión del usuario autenticado. Se hidrata desde localStorage al arrancar,
// así un F5 no bota la sesión. Si el token ya venció, el primer 401 de la API
// limpia todo y redirige a /login (lo hace el cliente HTTP).
export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(obtenerToken())
  const usuario = ref<UsuarioDto | null>(obtenerUsuario())

  const autenticado = computed(() => token.value !== null && usuario.value !== null)

  async function iniciarSesion(email: string, password: string): Promise<void> {
    const respuesta = await login(email, password)
    token.value = respuesta.accessToken
    usuario.value = respuesta.usuario
    guardarSesion(respuesta.accessToken, respuesta.usuario)
  }

  function cerrarSesion(): void {
    token.value = null
    usuario.value = null
    limpiarSesion()
  }

  return { token, usuario, autenticado, iniciarSesion, cerrarSesion }
})
