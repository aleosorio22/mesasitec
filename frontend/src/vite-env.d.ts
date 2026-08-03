/// <reference types="vite/client" />

// Tipado de las variables de entorno propias (para no caer en `any`).
interface ImportMetaEnv {
  readonly VITE_API_URL?: string
}
