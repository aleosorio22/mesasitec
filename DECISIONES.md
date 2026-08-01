# DECISIONES.md — MesaSitec

> Documento vivo. Se completa durante el proyecto.
> El enunciado (§8.4) pide como máximo 1 página con: 3 decisiones técnicas (con la
> alternativa descartada y por qué), qué se hizo con IA y qué a mano, qué se haría
> distinto con una semana más, y el punto donde me atasqué.

---

## 1. Tres decisiones técnicas

### Decisión 1 — .NET 8 en lugar de .NET 10
- **Qué elegí:** construir sobre .NET 8, fijado con un `global.json` que clava la versión `8.0.423`.
- **Alternativa descartada:** .NET 10 (también permitido por el enunciado y ya instalado).
- **Por qué:** hay más material de aprendizaje que calza exacto con .NET 8, y vengo de Node.js / web moderno (nuevo en .NET). Reduce el riesgo de perder tiempo con diferencias de versión durante una prueba de una semana.

### Decisión 2 — Controllers en lugar de Minimal API
- **Qué elegí:** Web API con controllers.
- **Alternativa descartada:** Minimal API.
- **Por qué:** encaja mejor con la separación por capas y hay más ejemplos que calzan con esa estructura.

### Decisión 3 — Máquina de estados a mano con diccionario `(Estado, Accion) -> Estado`
- **Qué elegí:** implementarla yo, con un diccionario como única fuente de verdad de las transiciones, en una clase `static` de lógica pura.
- **Alternativa descartada:** usar una librería de state machine, o un `switch` gigante.
- **Por qué:** el enunciado prefiere que la implemente uno mismo (§10). El diccionario deja las transiciones en un solo lugar y hace trivial agregar/quitar una si lo piden en la entrevista; queda obvio *dónde* tocar. `TryAplicar` es puro (retorna `bool` y entrega el destino por `out`), así que se prueba sin levantar la app (§5.2) — de hecho está cubierta por tests.

> _(Se puede sustituir alguna por una decisión posterior de mayor peso — p. ej. el enfoque de aislamiento por tenant en RN-01 — cuando llegue esa parte.)_

---

## 2. Qué hice con IA y qué a mano

- **Con IA (asistente conversacional):**
  - Andamiaje del modelado del Dominio (enums, entidades, esqueleto de la máquina de estados y la calculadora de SLA).
  - Diagnóstico de avisos del compilador (IDE0130 de namespaces, CS0103 por el enum faltante).
  - Redacción de la tanda de **pruebas unitarias** del Dominio (máquina de estados y SLA), a partir de mi código real; entendí cada patrón nuevo (`[Theory]`/`[InlineData]`, `out`, `DateTimeKind.Utc`) antes de aceptarlo.
  - Configuración del **DbContext y el mapeo de EF Core** (`OnModelCreating`): las dos relaciones `Solicitud → Usuario`, los índices únicos y el `DeleteBehavior`; comprendí el porqué de cada línea antes de compilar.
  - **Datos semilla** (`DatosSemilla.cs`): estructura, hasheo con BCrypt, fechas derivadas de `SEED_FECHA_BASE`, y la fábrica de solicitudes que reutiliza la `CalculadoraSla`. Revisé y corregí (p. ej. detecté que el correlativo debía ir alineado con la cronología, y ajustamos la fábrica para derivar la fecha del correlativo).
- **A mano / revisado y entendido por mí:** decisiones de entorno (IDE, versión de .NET), la estructura de capas y sus referencias, la corrección de cada tropiezo en el editor, la ejecución/verificación de los tests (`dotnet test`, 26/26 en verde) y de los datos semilla (SQLite CLI), y las decisiones de modelado de datos (enums como número, tenant sin navegación, BCrypt sobre PasswordHasher).
- **Compromiso:** entiendo lo que entrego; hay entrevista técnica con cambio en vivo, así que cada pieza queda anotada en la bitácora.

> _(Ampliar a medida que avanza el proyecto.)_

---

## 3. Qué haría distinto con una semana más

> _(Pendiente — completar cerca de la entrega.)_

---

## 4. El punto donde me atasqué y cómo lo resolví

> _(Pendiente — completar con un caso real. Candidato hasta ahora: el aviso IDE0130 de
> namespaces vs. estructura de carpetas, resuelto fijando `<RootNamespace>` en cada `.csproj`.)_

---

## Decisiones tomadas (candidatas a las 3 destacadas — reordenar cerca de la entrega)

- **Enums en SQLite → número.** Default de EF Core (cero configuración) y da el orden semántico de `Prioridad` gratis en el `ORDER BY`; como texto ordenaría alfabético (incorrecto) y exigiría traducción extra.
- **BCrypt sobre `PasswordHasher<T>`.** Ambos son seguros y el enunciado permite los dos. Elegí BCrypt (`BCrypt.Net-Next`) por familiaridad previa desde Node (mismo `bcrypt` del mundo Express) y por su API más directa (`HashPassword`/`Verify` estáticos, sin instanciar ni interpretar el enum de 3 estados de `PasswordHasher`). Alternativa descartada: `PasswordHasher<T>` (PBKDF2, cero dependencias externas).

> Al llegar a RN-01 (aislamiento por tenant) habrá otra decisión de peso. Cerca de la entrega, elegir las 3 más fuertes para la sección 1 (probablemente: máquina de estados, RN-01, y una de estas dos).

---

## Decisiones abiertas (por resolver en su capa)

- **Reabrir una solicitud (Resuelta → EnProceso):** el enunciado no dice si limpiar `FechaResolucion` / `MotivoResolucion`. Se decidirá en la capa `Aplicacion` y se documentará aquí.
- **Casing del namespace:** unificar `Mesasitec` / `MesaSitec` a una sola forma antes de que el proyecto crezca