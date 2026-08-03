import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import LoginView from '../views/LoginView.vue'
import SolicitudesListaView from '../views/SolicitudesListaView.vue'
import SolicitudDetalleView from '../views/SolicitudDetalleView.vue'
import SolicitudNuevaView from '../views/SolicitudNuevaView.vue'
import SolicitudEditarView from '../views/SolicitudEditarView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginView, meta: { publica: true } },
    { path: '/', redirect: '/solicitudes' },
    { path: '/solicitudes', name: 'solicitudes', component: SolicitudesListaView },
    { path: '/solicitudes/nueva', name: 'solicitud-nueva', component: SolicitudNuevaView },
    { path: '/solicitudes/:id', name: 'solicitud-detalle', component: SolicitudDetalleView },
    { path: '/solicitudes/:id/editar', name: 'solicitud-editar', component: SolicitudEditarView },
    { path: '/:pathMatch(.*)*', redirect: '/solicitudes' },
  ],
})

// Guard de rutas privadas (§7.1): sin sesión solo se puede estar en /login.
// Y con sesión, /login redirige al listado (no tiene sentido volver a entrar).
router.beforeEach((destino) => {
  const auth = useAuthStore()
  if (!destino.meta.publica && !auth.autenticado) return { name: 'login' }
  if (destino.name === 'login' && auth.autenticado) return { name: 'solicitudes' }
})

export default router
