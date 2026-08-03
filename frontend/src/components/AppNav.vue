<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

function salir(): void {
  auth.cerrarSesion()
  router.push('/login')
}
</script>

<template>
  <nav class="nav" data-testid="app-nav">
    <RouterLink to="/solicitudes" class="nav-marca">MesaSitec</RouterLink>
    <div v-if="auth.usuario" class="nav-usuario">
      <span class="nav-tenant">{{ auth.usuario.tenantNombre }}</span>
      <span data-testid="nav-usuario-nombre">{{ auth.usuario.nombre }}</span>
      <span class="chip chip-rol" data-testid="nav-usuario-rol">{{ auth.usuario.rol }}</span>
      <button class="boton boton-secundario" data-testid="btn-logout" @click="salir">Salir</button>
    </div>
  </nav>
</template>
