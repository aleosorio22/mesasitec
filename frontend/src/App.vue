<script setup lang="ts">
import { useAuthStore } from './stores/auth'
import AppNav from './components/AppNav.vue'
import ToastMensaje from './components/ToastMensaje.vue'

const auth = useAuthStore()
</script>

<template>
  <AppNav v-if="auth.autenticado" />
  <main class="contenido">
    <RouterView />
  </main>
  <ToastMensaje />
</template>
