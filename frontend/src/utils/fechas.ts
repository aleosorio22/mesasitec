// Las fechas de la API vienen en ISO-8601 UTC con Z; aquí se muestran
// en la zona horaria local del navegador, en formato corto legible.
export function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleString('es', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
