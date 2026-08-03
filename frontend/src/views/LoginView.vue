<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { ErrorApi } from '../api/http'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const error = ref<string | null>(null)
const enviando = ref(false)

async function entrar(): Promise<void> {
  error.value = null
  enviando.value = true
  try {
    await auth.iniciarSesion(email.value.trim(), password.value)
    router.push('/solicitudes')
  } catch (e) {
    error.value =
      e instanceof ErrorApi && e.status === 401
        ? 'Credenciales incorrectas. Revisa el correo y la contraseña.'
        : 'No se pudo iniciar sesión. ¿Está corriendo la API?'
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <div class="login-caja">
    <h1>MesaSitec</h1>
    <p class="login-subtitulo">Mesa de servicio</p>

    <form @submit.prevent="entrar">
      <label>
        Correo
        <input
          v-model="email"
          type="email"
          required
          autocomplete="username"
          placeholder="usuario@ejemplo.test"
          data-testid="login-email"
        />
      </label>

      <label>
        Contraseña
        <input
          v-model="password"
          type="password"
          required
          autocomplete="current-password"
          data-testid="login-password"
        />
      </label>

      <p v-if="error" class="error-texto" data-testid="login-error">{{ error }}</p>

      <button class="boton boton-primario" type="submit" :disabled="enviando" data-testid="login-submit">
        {{ enviando ? 'Entrando…' : 'Entrar' }}
      </button>
    </form>
  </div>
</template>
