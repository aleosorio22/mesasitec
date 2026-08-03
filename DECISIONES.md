# DECISIONES.md — MesaSitec

## 1. Tres decisiones técnicas

### Máquina de estados a mano, con un diccionario `(Estado, Accion) -> Estado`
- **Alternativa descartada:** una librería de state machine, o un `switch` gigante.
- **Por qué:** el enunciado prefiere implementación propia (§10). El diccionario deja las transiciones en un solo lugar y hace trivial agregar o quitar una; queda obvio *dónde* tocar. `TryAplicar` es puro (retorna `bool` y entrega el destino por `out`), así que se prueba sin levantar la app — está cubierta por tests.

### RN-01 apoyado en el `tenantId` del token, filtrando explícitamente en cada consulta
- **Alternativa descartada:** query filters globales de EF Core.
- **Por qué:** el `tenantId` se lee de los claims del JWT (nunca de la URL) y se aplica como primer `.Where()` de toda consulta. Los filtros globales son más "mágicos"; con el filtro manual el aislamiento queda visible en cada consulta, más fácil de auditar y de defender. La extracción de claims se centralizó en `ApiControllerBase`.

### Reglas RN-02/RN-03 replicadas en el frontend (`utils/reglas.ts`)
- **Alternativa descartada:** que la API devuelva las acciones disponibles por solicitud.
- **Por qué:** §7.5 exige que los botones no permitidos **no existan en el DOM**, así que el cliente necesita conocer las reglas. Devolverlas desde la API sería más limpio a largo plazo (una sola fuente de verdad), pero implica extender el contrato. El espejo es pequeño, está en un solo archivo, y la autoridad sigue siendo el backend: si el cliente se equivocara, la API responde 403/409 igual.

## 2. Otras decisiones (resumen)

- **Backend:** .NET 8 sobre .NET 10 (más material que calza exacto; vengo de Node). Controllers sobre Minimal API. BCrypt sobre `PasswordHasher<T>` (familiaridad desde Node). `DbContext` directo en servicios, sin patrón repositorio (los contratos viven en Aplicacion; sobre-ingeniería para una semana). Enums como número en SQLite (orden semántico de prioridad gratis). Correlativo RN-07 "contar + 1" sin blindaje de concurrencia (el enunciado lo exime). Editar fuera del estado permitido responde **403 `OPERACION_NO_PERMITIDA`** (lectura literal de RN-03; descarté 409/422); Admin/Agente pueden editar en `Nueva`/`Asignada`/`EnProceso`, el Solicitante solo en `Nueva`. `reabrir` **no** limpia `fechaResolucion`/`motivoResolucion`: conservan la última resolución como historial (el enunciado no lo define). Errores de validación vía `InvalidModelStateResponseFactory` para cumplir el 422 `VALIDACION` del contrato en vez del 400 default de ASP.NET.
- **Endpoint extra al contrato:** `GET /usuarios/agentes` (id y nombre de agentes/admins activos del tenant). El `modal-select-agente` de §7.4 necesita esa lista y el contrato no da forma de obtenerla; la FAQ permite agregar funcionalidad declarándola. Descarté deducir los agentes desde el listado de solicitudes (incompleto y frágil).
- **Frontend:** cliente HTTP propio con `fetch` en vez de axios (una dependencia menos; el wrapper son ~80 líneas y tipa los errores del contrato como `ErrorApi`). DTOs escritos a mano en `types/api.ts` en vez de generados del OpenAPI (con más tiempo los generaría; a mano son pocos y me obligaron a leer el contrato campo por campo). Enums del dominio como union types de literales, no `enum` de TS (el template usa `erasableSyntaxOnly`, y el union type ES el contrato serializado). Sesión en `localStorage` con hidratación del store al arrancar (un F5 no bota la sesión); el 401 hace redirección dura a `/login` desde el cliente HTTP, en un módulo `sesion.ts` aparte para evitar imports circulares entre http, store y router. Un solo modal genérico para las 6 acciones del flujo, que muestra select de agente o textarea de motivo según la acción, con la validación de RN-05/RN-06 espejada en el cliente. Debounce único de 300 ms para todos los filtros del listado (absorbe el tecleo de la búsqueda y evita recargas dobles al limpiar filtros). Filtro de vencidas como checkbox ("solo vencidas" o nada), no tri-estado. Puerto 5173 con `strictPort` para fallar ruidosamente si está ocupado (el CORS solo permite ese origen).

## 3. Qué hice con IA y qué a mano

Usé IA (asistente conversacional) durante todo el proyecto, de forma intensiva: andamiaje del dominio, mapeo de EF Core, datos semilla, JWT, la tanda de pruebas unitarias, el manejador global de errores y su alineación con el contrato (allí detectó, probando con `curl`, que el 401 del middleware salía vacío y que `WriteAsJsonAsync` pisaba el `Content-Type`), y el frontend completo (estructura, vistas, cliente HTTP), que además se verificó con pruebas automatizadas en el navegador contra la API real (login, filtros, paginación, transiciones, RN-01 entre tenants). A mano y con criterio propio: las decisiones de entorno y de modelado, la estructura de capas, la elección de qué implementar y en qué orden, la revisión de cada pieza antes de aceptarla y la bitácora donde fui anotando todo. El trato conmigo mismo fue no aceptar código que no pudiera explicar; hay entrevista con cambio en vivo y cuento con eso.

## 4. Qué haría distinto con una semana más

- Tests de integración de la API con `WebApplicationFactory` (hoy los 45 tests cubren el dominio) y E2E con Playwright sobre los `data-testid`.
- Devolver las acciones disponibles desde la API y eliminar el espejo de reglas del frontend.
- Generar los tipos TS desde el OpenAPI. Correlativo RN-07 con secuencia real. `docker-compose`. Paginación en SQL también cuando se filtra por vencidas (hoy ese caso pagina en memoria).

## 5. El punto donde me atasqué y cómo lo resolví

Al inicio, el analizador marcaba IDE0130 (namespace no coincide con la carpeta) por toda la solución y no entendía por qué: la estructura `src/{Api,Aplicacion,...}` no calzaba con los namespaces que quería usar. Lo resolví fijando `<RootNamespace>` en cada `.csproj`, y de paso entendí cómo .NET deriva namespaces de la estructura física. El segundo tropiezo fue silencioso y peor: el contrato exige `application/problem+json` y las respuestas salían como `application/json` aunque yo fijaba el `Content-Type` antes de escribir — resulta que `WriteAsJsonAsync` lo sobreescribe y hay que pasárselo como argumento. Lo cazamos solo porque las pruebas con `curl` imprimían el content-type real; la lección que me llevo es verificar contra el contrato con peticiones reales, no confiar en que el código "se ve bien".
