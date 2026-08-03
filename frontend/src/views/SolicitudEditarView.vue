<script setup lang="ts">
import { useRoute } from 'vue-router'
import SolicitudForm from '../components/SolicitudForm.vue'

const route = useRoute()
const solicitudId = String(route.params.id)
</script>

<template>
  <SolicitudForm modo="editar" :solicitud-id="solicitudId" />
</template>
