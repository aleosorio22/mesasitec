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
  - **Autenticación JWT:** generador de tokens, validación en `Program.cs`, servicio de login con BCrypt, y los DTOs como `records`. Entendí la teoría (firma, claims, por qué el secreto es crítico) antes de cablear, y el patrón de interfaces (`IGeneradorTokens`, `IServicioAuth`) para respetar el flujo de dependencias.
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
- **`DbContext` directo en los servicios, sin patrón repositorio.** Los servicios de aplicación (empezando por `ServicioAuth`) usan el `MesaSitecDbContext` directamente en vez de abstraer el acceso a datos tras interfaces de repositorio. Decisión consciente para no sobre-ingenierizar dado el alcance de una semana. Consecuencia de diseño: la implementación del servicio vive en **Infraestructura** (que ya referencia el DbContext), y solo su **contrato** (`IServicioAuth`) vive en **Aplicacion** — así el flujo de dependencias hacia adentro se mantiene intacto y `Api` depende del contrato, no de la implementación. En un sistema mayor: interfaces de repositorio en la capa de Aplicación. Alternativa descartada: patrón repositorio completo (más limpio, pero sobrado para este alcance).
- **RN-01 (aislamiento) apoyado en el `tenantId` del token, filtrando en cada consulta.** El `tenantId` se lee de los claims del JWT (no de la URL ni de parámetros que el usuario controle) y se aplica como primer `.Where()` en toda consulta de solicitudes. Para no repetir la extracción de claims en cada controller, se centralizó en una clase base `ApiControllerBase` (propiedades `TenantIdActual`/`UsuarioIdActual`/`RolActual`). Alternativa descartada: query filters globales de EF Core (más "mágicos" pero menos explícitos; con el filtro manual queda visible en cada consulta que el aislamiento se aplica, lo cual es más fácil de auditar y defender).
- **`record` para DTOs de salida, `class` para DTOs de entrada validados.** Los DTOs de respuesta son `records` (inmutables, concisos). Pero el DTO de entrada (`CrearSolicitudRequest`) se hizo `class` con propiedades `{ get; set; }`, porque la validación por Data Annotations sobre records posicionales daba conflicto en runtime (la metadata debe ir en el parámetro del constructor) y porque el model binding de ASP.NET necesita setters. Alternativa descartada: forzar records con `[property:]` (frágil, dio excepción).
- **Correlativo "contar y sumar 1", sin blindaje de concurrencia.** El código `SOL-{año}-{n}` se genera contando las solicitudes del tenant en el año + 1. El enunciado (RN-07) exime explícitamente de hacerlo infalible ante peticiones simultáneas, así que se optó por la vía simple en vez de una secuencia con bloqueo. Alternativa descartada: tabla de secuencias / lock (sobrado para el alcance).

> Cerca de la entrega, elegir las 3 más fuertes para la sección 1 (probablemente: máquina de estados, RN-01/aislamiento, y BCrypt o DbContext directo).

---

## Decisiones abiertas (por resolver en su capa)

- **Reabrir una solicitud (Resuelta → EnProceso):** el enunciado no dice si limpiar `FechaResolucion` / `MotivoResolucion`. Se decidirá en la capa `Aplicacion` y se documentará aquí.
- **Casing del namespace:** unificar `Mesasitec` / `MesaSitec` a una sola forma antes de que el proyecto crezca.