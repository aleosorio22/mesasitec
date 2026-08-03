import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    // El enunciado exige el 5173 (y el CORS del backend solo permite ese origen).
    // strictPort: si está ocupado, falla en vez de saltar al 5174 en silencio.
    port: 5173,
    strictPort: true,
  },
})
