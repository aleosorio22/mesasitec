# MesaSitec

Mesa de servicio multi-tenant: varias organizaciones comparten la misma aplicación y la misma base de datos, sin poder ver los datos de las otras. Los usuarios crean solicitudes de soporte, un agente las atiende siguiendo una máquina de estados, y cada solicitud tiene un plazo de atención (SLA) que el servidor calcula según la categoría y la prioridad.

Prueba técnica para Sitecpro.

## Estado actual

- **Backend: completo.** Los 9 endpoints del contrato, las reglas RN-01 a RN-07 y 45 pruebas unitarias en verde.
- **Frontend: completo.** Las 5 vistas del enunciado con sus `data-testid`, estados de carga/vacío/error y guard de rutas.

El detalle de qué hay y qué falta está al final de este archivo.

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20.19 o superior](https://nodejs.org) (con npm)

Nada más. La base de datos es SQLite (un archivo local que se crea solo), así que no hay que instalar ni configurar ningún motor de base de datos.

## Cómo levantar el proyecto

En una terminal, el backend:

```bash
cd backend/src/Api
dotnet run
```

En otra terminal, el frontend:

```bash
cd frontend
npm install && npm run dev
```

Eso es todo. Al arrancar, la API aplica las migraciones y siembra los datos de prueba automáticamente si la base está vacía. Queda disponible:

- Frontend: `http://localhost:5173`
- API: `http://localhost:5080/api/v1`
- Swagger: `http://localhost:5080/swagger`
- Health check (sin autenticación): `http://localhost:5080/api/v1/health`

Para probar los endpoints protegidos desde Swagger: hacer login en `POST /auth/login`, copiar el `accessToken` de la respuesta y pegarlo en el botón "Authorize" (sin escribir la palabra Bearer).

## Credenciales de prueba

Todos los usuarios semilla usan la contraseña **`Sitec.2026`**.

| Email | Organización | Rol |
|---|---|---|
| `admin@norte.test` | Cooperativa Norte | Admin |
| `agente1@norte.test` | Cooperativa Norte | Agente |
| `agente2@norte.test` | Cooperativa Norte | Agente |
| `user1@norte.test` | Cooperativa Norte | Solicitante |
| `user2@norte.test` | Cooperativa Norte | Solicitante |
| `admin@sur.test` | Bufete Sur | Admin |
| `user1@sur.test` | Bufete Sur | Solicitante |

La semilla crea 25 solicitudes en Cooperativa Norte y 8 en Bufete Sur, repartidas entre todos los estados y prioridades.

## Variables de entorno

No hace falta configurar nada para desarrollo: hay valores por defecto que funcionan. Si se quieren cambiar, en `.env.example` están documentadas:

| Variable | Para qué | Default |
|---|---|---|
| `JWT__SECRET` | Secreto con el que se firman los tokens (HS256) | Hay uno de desarrollo en `appsettings.Development.json` |
| `SEED_FECHA_BASE` | Fecha de referencia de los datos semilla (todas las fechas se generan como desplazamientos fijos respecto a ella) | `2026-01-15T08:00:00Z` |

## Correr las pruebas

```bash
cd backend
dotnet test
```

Son 45 pruebas unitarias (xUnit) sobre la lógica del dominio: máquina de estados (RN-02), permisos por rol (RN-03) y cálculo del SLA (RN-04).

El chequeo de tipos del frontend (TypeScript en modo `strict`, sin `any`):

```bash
cd frontend
npm run typecheck
```

## Estructura del proyecto

```
backend/
├─ src/
│  ├─ Api/              controllers, manejo de errores, Program.cs
│  ├─ Aplicacion/       contratos de servicios y DTOs
│  ├─ Dominio/          entidades, enums y reglas de negocio puras
│  └─ Infraestructura/  EF Core, migraciones, semilla, servicios, JWT
└─ tests/
   └─ Tests/            pruebas unitarias del dominio

frontend/
└─ src/
   ├─ api/              cliente HTTP único, sesión y funciones por recurso
   ├─ components/       navegación, toast y el formulario de solicitud
   ├─ views/            login, listado, detalle, crear y editar
   ├─ stores/           Pinia: auth y toast
   ├─ types/            DTOs del contrato tipados
   ├─ router/           rutas y guard de autenticación
   └─ utils/            reglas RN-02/RN-03 en el cliente, constantes, fechas
```

La máquina de estados, el cálculo del SLA y los permisos por rol viven en `Dominio/Reglas` como clases estáticas puras, sin dependencia de EF ni de ASP.NET. Por eso se pueden probar sin levantar la aplicación. El frontend replica esas dos reglas en `utils/reglas.ts` solo para decidir qué botones renderizar (§7.5); quien manda siempre es el backend.

## Qué está implementado y qué no

Implementado en el backend:

- Los 9 endpoints del contrato (`/auth/login`, `/me`, `/categorias`, CRUD de solicitudes, transiciones, `/health`).
- Aislamiento por tenant (RN-01): un recurso de otra organización responde 404, nunca 403.
- Máquina de estados (RN-02) implementada a mano, sin librerías.
- Permisos por rol (RN-03), cálculo y recálculo del SLA (RN-04), validación de agente al asignar (RN-05), motivos obligatorios al resolver/cancelar (RN-06) y código correlativo por organización y año (RN-07).
- Formato de error unificado `application/problem+json` con el campo `codigo` en todos los errores, incluidos los 401 del middleware JWT y los errores de validación automática.
- JWT HS256 con expiración de 8 horas, contraseñas con BCrypt, Swagger con esquema Bearer, CORS para `http://localhost:5173`, manejador global de excepciones.
- Migraciones y semilla automáticas al arrancar.
- Un endpoint extra al contrato: `GET /usuarios/agentes` (agentes/admins activos del tenant). El modal de asignación lo necesita y el contrato no da otra forma de obtener esa lista. Justificado en `DECISIONES.md`.

Implementado en el frontend:

- Las 5 vistas (§7.3): login, listado con filtros/búsqueda/paginación server-side, detalle con acciones por estado y rol, y el mismo formulario para crear y editar.
- Todos los `data-testid` de §7.4. Los botones de acción no permitidos no se renderizan en el DOM (§7.5).
- Cada vista maneja sus estados de cargando, vacío y error (§7.2).
- Cliente HTTP único que inyecta el token y redirige a `/login` ante un 401.
- TypeScript `strict` sin `any`; `npm run typecheck` pasa limpio.

Pendiente:

- `docker-compose.yml` (opcional en el enunciado).

Decisiones tomadas ante ambigüedades del enunciado (por ejemplo, en qué estados puede editar un Admin/Agente) están en `DECISIONES.md`.
