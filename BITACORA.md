# Bitácora — Proyecto MesaSitec

> Prueba técnica Junior (.NET 8). Bitácora **general y única**: consolida todo lo hecho
> y se va actualizando a medida que avanza el proyecto. Sirve para retomar el trabajo
> en un chat nuevo sin perder contexto.
>
> Reemplaza a las antiguas `BITACORA01.md` (setup) y `BITACORA02.md` (modelado del Dominio).

---

## Contexto del proyecto

- **Qué es:** una mesa de servicio SaaS multi-tenant. Varias organizaciones comparten la misma app y la misma base de datos, pero **jamás** deben ver datos de otra (regla más importante: **RN-01**, aislamiento por `tenantId`).
- **Stack backend:** .NET 8, EF Core, SQLite, JWT propio (HS256), BCrypt, Swagger.
- **Stack frontend:** Vue 3 + `<script setup>`, TypeScript strict, Vite (puerto 5173), Vue Router, Pinia.
- **Perfil del desarrollador:** vengo de Node.js y web moderno; nuevo en .NET.
- **Repo:** `github.com/aleosorio22/mesasitec` (público, rama `main`).
- **Entrega:** conceder acceso al colaborador `osanchezm` en GitHub (ojo: usuario con `m` final, distinto del correo `osanchez@sitecpro.com`).

---

## Entorno de desarrollo (verificado ✅)

Todo instalado y funcionando en macOS:

| Herramienta | Versión | Estado |
|---|---|---|
| SDK .NET | 8.0.423 y 10.0.302 | ✅ Ambos instalados (se usa el 8) |
| Node.js | v24.16.0 | ✅ |
| npm | 11.13.0 | ✅ |
| Git | 2.54.0 | ✅ |
| `dotnet-ef` | 10.0.10 | ✅ Herramienta global |
| IDE | VS Code + C# Dev Kit | ✅ |

**Sobre el IDE:** VS Code + extensión **C# Dev Kit** (arrastra la extensión base de C# y el runtime). Motivo: ya conozco el editor viniendo de Node, y el frontend es Vue + TypeScript, así backend y frontend viven en el mismo sitio. Todo funciona igual desde terminal (`dotnet build`, `dotnet test`, `dotnet ef`). Rider queda como plan B. Nota: C# Dev Kit pide iniciar sesión con cuenta Microsoft (licencia gratis para individuos).

---

## FASE 1 — Setup del repositorio y la solución ✅

### 1. Control de versiones
- Repo local (`git init`) + remoto público en GitHub. Rama `main` enlazada, primer push OK.
- Commit inicial: `chore: estructura inicial del backend`.

### 2. `global.json` (raíz) — clava el repo a .NET 8
```json
{
  "sdk": {
    "version": "8.0.423",
    "rollForward": "latestFeature"
  }
}
```

### 3. `.gitignore`
- Base con `dotnet new gitignore` (plantilla oficial).
- Añadido a mano: `node_modules/`, `*.db`, `*.db-shm`, `*.db-wal`, `.env`.
- Cubre lo que el enunciado prohíbe versionar: `bin/`, `obj/`, `node_modules/`, `.db`, secretos.

### 4. Estructura del backend por capas
Solución `MesaSitec.sln` con cinco proyectos:

```
backend/
├─ MesaSitec.sln
├─ src/
│  ├─ Api/              (webapi con controllers — puerta de entrada HTTP, ejecutable)
│  ├─ Aplicacion/       (classlib — casos de uso, orquestación)
│  ├─ Dominio/          (classlib — entidades, enums, máquina de estados, SLA. No depende de nadie)
│  └─ Infraestructura/  (classlib — EF Core, SQLite, BCrypt, JWT)
└─ tests/
   └─ Tests/            (xUnit — pruebas unitarias)
```

**Referencias (dependencias apuntando hacia adentro):**
- `Aplicacion` → `Dominio`
- `Infraestructura` → `Aplicacion`, `Dominio`
- `Api` → `Aplicacion`, `Infraestructura`
- `Tests` → `Dominio`, `Aplicacion`

Nadie apunta hacia `Api`. El `Dominio` no depende de nadie. Esto obliga por diseño a que la lógica de negocio no viva en los controllers (requisito del enunciado §5.2).

**Estado:** `dotnet build` en verde — 0 errores, 0 advertencias.

---

## FASE 2 — Modelado del Dominio ✅

### 1. Enums (`src/Dominio/Enums/`)
- `Rol { Admin, Agente, Solicitante }`
- `Prioridad { Baja, Media, Alta, Critica }` — **el orden importa**: Baja=0 < Media=1 < Alta=2 < Critica=3. Da gratis el orden semántico que pide el listado (Critica > Alta > Media > Baja).
- `Estado { Nueva, Asignada, EnProceso, Resuelta, Cerrada, Cancelada }`
- `Accion { Asignar, Iniciar, Resolver, Cerrar, Reabrir, Cancelar }` — vocabulario del flujo; la API recibe strings y los parsea a este enum antes de tocar el dominio.

### 2. Entidades (`src/Dominio/Entidades/`)
- `Tenant` (id, nombre, activo)
- `Usuario` (id, tenantId, email, passwordHash, nombre, rol, activo) + navegación a `Tenant`
- `Categoria` (id, tenantId, nombre, slaHoras, activo)
- `Solicitud` (todos los campos del enunciado) + navegaciones a `Categoria`, `Solicitante` y `Agente`.

### 3. Máquina de estados (`src/Dominio/Reglas/MaquinaEstados.cs`) — RN-02
- Implementada **a mano** (el enunciado prefiere no usar librería).
- **Clase `static`**: diccionario `(Estado, Accion) -> Estado destino` como única fuente de verdad, trivial de extender.
- Métodos: `EsTransicionValida`, `TryAplicar` (puro, sin excepciones, devuelve el destino por parámetro `out`), `AccionesPermitidas` (útil para saber qué botones existen en el frontend).
- `Cerrada` y `Cancelada` son finales: no tienen entradas → nada es válido desde ellas.

### 4. Cálculo del SLA (`src/Dominio/Reglas/CalculadoraSla.cs`) — RN-04
- **Clase `static`**, lógica pura: recibe `ahora` por parámetro en lugar de llamar a `DateTime.UtcNow` adentro → testeable con fechas fijas.
- `CalcularFechaLimite(fechaCreacion, slaHoras, prioridad)` con factores { Critica 0.5, Alta 0.75, Media 1.0, Baja 2.0 }.
- `EstaVencida(fechaLimiteSla, estado, ahora)`: vencida si el límite ya pasó **Y** el estado no es Resuelta/Cerrada/Cancelada.

### Tropiezos resueltos en esta fase
1. **Aviso IDE0130 "namespace does not match folder structure".** No es error, es sugerencia del analizador (los proyectos se llaman `Dominio`, `Aplicacion`... pero el código usa el prefijo del producto). **Solución:** agregar `<RootNamespace>` en cada `.csproj`.
2. **Consistencia de casing en el namespace.** Se detectó `Mesasitec` vs `MesaSitec`. C# es case-sensitive → una sola forma en todo el proyecto. *(Ver "Riesgos abiertos" abajo: sigue pendiente unificar.)*
3. **CS0103 `The name 'Accion' does not exist`.** Faltaba crear `Accion.cs`. Al crearlo desaparecieron los 11 errores de `MaquinaEstados.cs` (todos eran el mismo tipo faltante).

---

## FASE 3 — Pruebas unitarias del Dominio ✅

### Qué se hizo
- Se creó el proyecto de tests con xUnit (equivalente a Jest/Vitest en Node). Se eliminó el placeholder `UnitTest1.cs` de la plantilla.
- Dos archivos de prueba en `tests/Tests/`:
  - **`MaquinaEstadosTests.cs`** (RN-02):
    - `[Theory]` con las 10 transiciones **válidas** de la tabla RN-02 → 10 tests.
    - `[Theory]` con transiciones **inválidas**, incluidos los dos estados finales (`Cerrada`, `Cancelada`) → 5 tests.
    - 2 `[Fact]` sobre `AccionesPermitidas` (Nueva devuelve Asignar+Cancelar; Cerrada devuelve vacío).
  - **`CalculadoraSlaTests.cs`** (RN-04):
    - `[Theory]` con los 4 factores de prioridad, **incluyendo los dos ejemplos textuales del enunciado** (Incidente 8h crítica → 4h; Consulta 24h baja → 48h) → 4 tests.
    - `[Fact]` vencida (límite pasado + estado vivo).
    - `[Theory]` NO vencida por estado final/resuelto (Resuelta, Cerrada, Cancelada) → 3 tests.
    - `[Fact]` NO vencida porque el límite aún no llega.

### Resultado
`dotnet test` (desde `backend/`) → **26 superadas, 0 con error, 9 ms.**
Con esto se cubren **2 de las 3** áreas que exige §5.4 (máquina de estados y SLA) y se supera de sobra el mínimo de 8 pruebas.

### Conceptos nuevos aprendidos (desde Node)
- **xUnit ≈ Jest/Vitest.** `[Fact]` ≈ `test()`; `[Theory]` + `[InlineData]` ≈ `test.each()`; `Assert.Equal(esperado, actual)` ≈ `expect(actual).toBe(esperado)` **pero el esperado va primero**.
- **Parámetro `out`:** patrón `TryHacerAlgo` de .NET — el método retorna `bool` de éxito y "devuelve" el resultado rellenando una variable `out`. `out _` descarta el valor cuando no interesa.
- **`DateTimeKind.Utc`:** marcar las fechas como UTC desde su construcción evita bugs de zona horaria (el enunciado exige UTC con sufijo `Z`).
- **`DateTime` es inmutable:** `AddHours(n)` no muta, devuelve una fecha nueva.

### Commit sugerido
```
test: pruebas unitarias del dominio (maquina de estados RN-02 y SLA RN-04)
```

---

## FASE 4 — EF Core + DbContext (Infraestructura) 🚧 en curso

### Qué es (mapa mental desde Node)
- **EF Core** = el ORM de .NET ≈ **Prisma / TypeORM**. Traduce entre objetos C# y tablas SQL.
- Enfoque **code-first**: no hay archivo de esquema aparte (como el `.prisma`); **las clases de C# SON el esquema**. EF Core las lee por reflexión.
- **`DbContext`** = la "sesión" con la BD, puerta de entrada ≈ cliente de Prisma / `DataSource` de TypeORM.
- **`DbSet<T>`** = una tabla vista como colección consultable; sobre él se hace LINQ (`.Where`, `.OrderBy`) y EF Core lo traduce a SQL ≈ `prisma.solicitud`.
- **SQLite** = motor de BD en un simple archivo `.db` local (no hay que instalar nada — requisito del enunciado).

### Paquetes NuGet instalados (NuGet ≈ registro de npm)
- `Microsoft.EntityFrameworkCore.Sqlite` (v8.*) → en **Infraestructura** (proveedor de SQLite; arrastra el núcleo de EF Core).
- `Microsoft.EntityFrameworkCore.Design` (v8.*) → en **Api** (herramientas para generar migraciones; van en el proyecto de arranque).
- Se fijó `--version 8.*` para no traer EF Core 9/10. La herramienta global `dotnet-ef` es 10.0.10 → **operó sobre el proyecto 8 sin quejarse** (no hizo falta fijar herramienta local).

### DbContext (`src/Infraestructura/Data/MesaSitecDbContext.cs`)
- Vive en carpeta `Data` con `namespace Mesasitec.Infraestructura.Data`.
- Hereda de `DbContext`. Constructor recibe `DbContextOptions` **inyectadas desde fuera** (no configura SQLite adentro → agnóstico del proveedor y más testeable).
- Cuatro `DbSet`: `Tenants`, `Usuarios`, `Categorias`, `Solicitudes` (sintaxis `=> Set<T>()`).

### Mapeo del modelo (`OnModelCreating`, Fluent API ≈ `@unique`/`@relation` de Prisma)
- **`Usuario.Email`**: índice **único global** (`HasIndex(...).IsUnique()`) — así lo exige el enunciado.
- **`Solicitud` código**: índice único **compuesto** `(TenantId, Codigo)` — RN-07: el correlativo es único por tenant y por año, así que el mismo string puede repetirse entre organizaciones. Único global sería incorrecto.
- **Dos relaciones `Solicitud` → `Usuario`** (el punto delicado): `Solicitante` (obligatorio) y `Agente` (opcional, `AgenteId` es `Guid?`). Con dos caminos a la misma tabla, la convención automática no da abasto → se emparejan a mano con `HasOne(...).WithMany().HasForeignKey(...)`.
- **`OnDelete(DeleteBehavior.Restrict)`** en las relaciones: evita el error de "múltiples rutas de borrado en cascada" y encaja con el proyecto (nunca se borran usuarios físicamente; se desactivan con el flag `Activo` → borrado lógico).

### Decisión tomada — enums como NÚMERO
- Se guardan como su valor numérico (default de EF Core → **cero configuración**).
- Ventaja funcional concreta: `Prioridad` está definido en orden semántico (Baja=0 … Critica=3), así el `ORDER BY prioridad` del listado sale en el orden correcto **gratis** (Critica > Alta > Media > Baja). Guardarlo como texto ordenaría alfabético (mal) y exigiría traducción extra.

### Decisión tomada — `TenantId` sin navegación a `Tenant` en Categoria/Solicitud
- `Categoria` y `Solicitud` tienen `TenantId` como `Guid` simple, **sin** navegación ni FK a `Tenant`. Es intencional: el aislamiento (RN-01) se hará **filtrando manualmente por `TenantId`** en cada consulta (lo que pide el enunciado), no vía navegación. Mantiene el modelo simple. (Explicable en entrevista como decisión, no olvido.)

### Migración inicial (`InitialCreate`)
- Generada con `dotnet ef migrations add InitialCreate --project src/Infraestructura --startup-project src/Api`.
  - `--project` = dónde se **guardan** los archivos (Infraestructura).
  - `--startup-project` = qué proyecto **arranca** la herramienta para leer la config de SQLite del `Program.cs` (Api).
- Creó `src/Infraestructura/Migrations/` con 3 archivos: la migración (`Up()`/`Down()`), su `.Designer.cs` y el `ModelSnapshot`. **Se versiona** (es historia del esquema); el `.db` NO.
- Verificado en el `Up()`: 4 tablas, los 3 FK de `Solicitudes` (Categoria + Solicitante + Agente con nombres distintos), índice único `Email`, índice único compuesto `(TenantId, Codigo)`. Enums salen como `INTEGER` (decisión "número" hecha realidad); Guid y DateTime salen como `TEXT` (SQLite no tiene tipo nativo para ellos → EF Core los serializa; en C# siguen siendo `Guid`/`DateTime`).

### Migraciones automáticas al arrancar (§5.1)
- Bloque en `Program.cs` justo tras `builder.Build()`:
  ```csharp
  using (var scope = app.Services.CreateScope())
  {
      var db = scope.ServiceProvider.GetRequiredService<MesaSitecDbContext>();
      db.Database.Migrate();
  }
  ```
- **Por qué el scope:** el `DbContext` se registra como *scoped* (vive por petición HTTP). Al arrancar no hay petición → se crea un scope manual con `CreateScope()`. El `using(...)` lo cierra al terminar.
- **`Migrate()`** crea el `.db` si no existe y aplica migraciones pendientes; idempotente (en el 2.º arranque ya no ejecuta `CREATE TABLE`). Distinto de `EnsureCreated()` (ese ignora migraciones; NO mezclar).
- Verificado: 1.er arranque creó `mesasitec.db` en `src/Api/` + tabla `__EFMigrationsHistory` (la "libreta" de migraciones aplicadas). `git status` confirma que el `.db` NO se versiona.

### Puerto fijado a 5080 (§6)
- La plantilla arrancaba en `5055`. Cambiado en `src/Api/Properties/launchSettings.json` → perfil `http` con `applicationUrl: http://localhost:5080` y `launchUrl: swagger`.
- Arrancar siempre con perfil explícito: `dotnet run --project src/Api --launch-profile http` (no depender de "el primer perfil de la lista"). Esto irá en el README.

### Estado
- Base de datos se crea y migra sola al arrancar ✅. API escuchando en `http://localhost:5080` ✅. Swagger accesible en `/swagger` ✅ (mostraba "No operations" hasta crear el primer controller).
- **Pendiente de la capa de datos:** datos semilla con `SEED_FECHA_BASE`.

### Commits hechos
```
feat(infra): agrega EF Core + SQLite y DbContext con mapeo del modelo
feat(infra): migracion inicial y aplicacion automatica al arrancar, puerto 5080
```

---

## FASE 5 — Primer endpoint: GET /health ✅

### Qué se hizo
- Primer controller: `src/Api/Controllers/HealthController.cs`. Endpoint #9 del contrato, **sin autenticación**, devuelve `{ "estado": "ok" }` con 200.
- Verificado en Swagger (aparece el bloque GET `/api/v1/health`, "Try it out" → 200) y con `curl http://localhost:5080/api/v1/health`.
- ✔️ Casilla del checklist: "GET /health responde 200 sin token".

### Anatomía de un controller (molde para todos los demás)
- `[ApiController]`: marca la clase como controller de API (validación automática, respuestas de error bien formadas, inferencia de parámetros).
- `[Route("api/v1/health")]`: ruta base literal, respetando el contrato (base `api/v1` + recurso).
- `: ControllerBase`: trae los helpers HTTP (`Ok()`, `NotFound()`, `BadRequest()`...). Es la versión sin vistas HTML (solo JSON).
- `[HttpGet]` sobre el método: responde a GET en la ruta base.
- `IActionResult`: tipo de retorno flexible = "cualquier respuesta HTTP".
- `Ok(new { estado = "ok" })`: 200 + objeto anónimo (≈ objeto literal JS) serializado a JSON.

### Nota sobre camelCase (gratis)
- ASP.NET Core serializa a **camelCase por defecto** (`FechaCreacion` → `fechaCreacion`). El contrato exige camelCase y se cumple solo, sin configurar nada.

### Commit hecho
```
feat(api): agrega endpoint GET /health (sin autenticacion)
```

---

## FASE 6 — Datos semilla (BCrypt + reproducibilidad) ✅

### Qué es y por qué
- Sembrar = rellenar la BD con datos iniciales la 1.ª vez que arranca (si está vacía). §6.3. Parte del "levantar en 5 min": quien clona el repo tiene datos sin tocar nada.
- Clase dedicada `src/Infraestructura/Data/DatosSemilla.cs` (método estático `Sembrar(db, fechaBase)`), llamada desde `Program.cs`. No se mete el sembrado dentro del `Program.cs`.

### Los 3 conceptos que se estrenaron aquí
1. **BCrypt** (hasheo de contraseñas). §5.1 descalifica texto plano/MD5/SHA1. Todos los usuarios semilla comparten `Sitec.2026`, pero en la BD solo va el **hash** (irreversible). Verificado en el log: el `PasswordHash` sale con `Size = 60` (largo de un hash BCrypt) y **nunca** aparece el texto plano.
2. **`SEED_FECHA_BASE`** (fechas reproducibles). Todas las fechas son **offsets fijos** respecto a la fecha base (`2026-01-15T08:00:00Z` por defecto), nunca `DateTime.UtcNow` → datos idénticos siembre quien siembre. Mismo principio que la `CalculadoraSla` (recibe el tiempo por parámetro).
3. **Idempotencia** (`if (db.Usuarios.Any()) return;`). Solo siembra si está vacía; en el 2.º arranque no duplica.

### Paquete instalado
- `BCrypt.Net-Next` (ojo: el `-Next`; `BCrypt.Net` a secas está abandonado) → en **Infraestructura**. Sin `--version` (es independiente del framework, la última estable va bien con .NET 8).

### Datos creados (§6.3)
- **2 tenants:** Cooperativa Norte, Bufete Sur.
- **7 usuarios:** 5 Norte (1 admin, 2 agentes, 2 solicitantes) + 2 Sur (1 admin, 1 solicitante). Emails y roles literales del enunciado; nombres inventados (no los verifica ninguna prueba).
- **8 categorías:** las 4 (Incidente 8h, Requerimiento 40h, Consulta 24h, Falla crítica 4h) en **ambos** tenants. Ojo tilde en "Falla crítica".
- **33 solicitudes:** 25 Norte + 8 Sur, repartidas por estados y prioridades. Norte: 4 resueltas (≥3 ✓) y sobran vencidas (≥5 ✓).

### Técnicas usadas
- **Guid generados a mano** (`Guid.NewGuid()` en variables) para poder enlazar usuarios→tenant y solicitudes→categoría antes de guardar.
- **Función local `CrearCategorias`** que devuelve `Dictionary<string, Categoria>` (mapa nombre→categoría) → permite referenciar `categoriasNorte["Incidente"]` al crear solicitudes.
- **Función fábrica `Crear(...)`** para las solicitudes: se encarga sola del código (`SOL-{año}-{correlativo:D5}`), del SLA (**reutiliza `CalculadoraSla`** del Dominio, ya testeada), y de la coherencia por estado (resueltas/cerradas → fecha+motivo de resolución; canceladas → motivo de cancelación).
- **Correlativo por `ref int`** independiente por tenant (Norte 1-25, Sur 1-8) → RN-07.
- **Un solo `SaveChanges()`** al final: EF Core acumula los `Add`/`AddRange` en memoria (como un carrito) y confirma todo en una transacción.

### Decisión de diseño — fecha derivada del correlativo
- Problema detectado en revisión: si las horas se asignaban a mano, un correlativo mayor podía tener fecha **anterior** (inconsistente).
- Solución: la fábrica calcula `horasAntesDeBase = (30 - correlativo) * 6`, así a mayor correlativo, más reciente. Cronología correcta **por construcción**, imposible de equivocar. Se quitó el parámetro manual de horas.

### Detalle RN-05 en Sur
- Bufete Sur no tiene rol `Agente`; su `Admin` actúa como agente en las solicitudes asignadas. Válido porque RN-05 permite agente con rol `Agente` **o** `Admin`.

### Cableado en `Program.cs`
- El bloque del scope ahora: `Migrate()` → lee `SEED_FECHA_BASE` de variable de entorno (`Environment.GetEnvironmentVariable`, default del enunciado) → parsea con `DateTime.Parse(..., AdjustToUniversal)` para **garantizar `DateTimeKind.Utc`** (para que salga con "Z" en el JSON) → `DatosSemilla.Sembrar(db, fechaBase)`.

### Logging de EF Core silenciado
- El sembrado escupía cientos de líneas de SQL. En `appsettings.json` → `Logging:LogLevel` se añadió `"Microsoft.EntityFrameworkCore.Database.Command": "Warning"` para no mostrar cada query (solo advertencias/errores).

### Verificación (SQLite CLI)
- `SELECT Codigo... ORDER BY Codigo` → códigos `SOL-2026-00001..00010` en orden, sin huecos → RN-07 ✅.
- Conteo por tenant → `Cooperativa Norte|25`, `Bufete Sur|8` ✅.
- Enums guardados como número (Estado 0=Nueva…, Prioridad 0=Baja…3=Critica) → decisión "número" confirmada en datos reales.

### Commits hechos
```
feat(infra): datos semilla con BCrypt y SEED_FECHA_BASE reproducible
chore(api): baja verbosidad de logs de EF Core
```

---

## Riesgos abiertos / deuda técnica (no perder de vista)

- **Realismo cronológico del flujo (menor, NO arreglar):** con la fecha derivada del correlativo, una solicitud "Cancelada" puede ser más reciente que una "Resuelta". El enunciado no pide simular el flujo temporal, solo cubrir estados/prioridades. Respuesta lista para entrevista: se priorizó cobertura de estados sobre simulación temporal.

- **Relación `Usuario → Tenant` quedó con `Cascade`** (no `Restrict`). Solo se configuró `Restrict` en las relaciones de `Solicitud`; la de Usuario→Tenant quedó con el default. Inofensivo para esta prueba (nunca se borran tenants ni usuarios; todo es borrado lógico con `Activo`). Si se quisiera uniformar, es 1 línea en `OnModelCreating`. No urge.

- **Casing del namespace `Mesasitec` vs `MesaSitec`.** El código real usa `Mesasitec` (segunda `s` minúscula); la solución se llama `MesaSitec.sln`. El nombre del `.sln` es cosmético y no afecta la compilación, pero conviene unificar a **una sola forma** antes de que el proyecto crezca. Decidir cuál y aplicarla de una vez.
- **RN-03 (permisos por rol) sin tests todavía.** Es la 3.ª área que pide §5.4. No se puede testear aún porque esa lógica vivirá en la capa `Aplicacion` (aún no existe). Retomar al montar los casos de uso.

---

## Decisiones abiertas (por resolver en su capa)

- ~~**Enums en SQLite:** número o texto.~~ ✅ **RESUELTO (Fase 4): NÚMERO** (default de EF Core; da orden semántico gratis para `Prioridad`).
- **Reabrir (Resuelta → EnProceso):** el enunciado no dice si limpiar `FechaResolucion` / `MotivoResolucion`. Se decidirá en `Aplicacion` y se documentará.

---

## Siguientes pasos (orden sugerido §9)

1. **EF Core + migración + arranque automático + puerto 5080 + datos semilla** ✅ hecho. **GET /health** ✅ hecho. **Capa de datos TERMINADA.**
2. **Login con JWT + `/me`.** ← **AQUÍ VAMOS**
3. **`GET /solicitudes` con filtro por tenant (RN-01).** La regla más importante; hacerla bien desde el inicio.
4. **Pruebas de permisos (RN-03)** cuando exista `Aplicacion` → cierra la 3.ª área de §5.4.
5. **Resto de endpoints** (9 en total).
6. **Frontend** (Vue): login → listado → detalle → formulario. Con `data-testid` literales y estados cargando/vacío/error.
7. **README, DECISIONES.md y limpieza** (mínimo 8 commits significativos, sin `bin/`/`obj/`/`node_modules/`/`.db`).

---

## Recordatorios clave del enunciado (no perder de vista)

- **RN-01 (aislamiento por tenant)** es la regla más importante. Recurso de otra organización → **404**, no 403.
- **Contrato de API literal:** rutas, nombres de campo en `camelCase`, códigos de respuesta exactos. Las pruebas automáticas son estrictas.
- **Todos los errores** usan `application/problem+json` con campo `codigo` obligatorio.
- **El proyecto debe levantar en < 5 minutos siguiendo el README** — requisito eliminatorio.
- **`data-testid` del frontend** copiados literalmente; uno faltante = funcionalidad no entregada.
- **Botones de acción no permitidos NO deben existir en el DOM** (no basta con ocultarlos).
- **Uso de IA permitido**, pero declarado en `DECISIONES.md` y entendiendo lo que se entrega (habrá entrevista técnica con cambio en vivo).

---

*Última actualización: capa de datos TERMINADA — BD migrada y sembrada (2 tenants, 7 usuarios, 8 categorías, 33 solicitudes) con BCrypt y fechas reproducibles, verificado por SQLite CLI. Siguiente: login con JWT + /me.*