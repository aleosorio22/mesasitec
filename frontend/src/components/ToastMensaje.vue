<script setup lang="ts">
import { useToastStore } from '../stores/toast'

const toast = useToastStore()
</script>

<template>
  <div
    v-if="toast.mensaje"
    class="toast"
    :class="toast.tipo === 'error' ? 'toast-error' : 'toast-exito'"
    data-testid="toast-mensaje"
  >
    {{ toast.mensaje }}
  </div>
</template>
