# MesaSitec

Mesa de servicio multi-tenant: varias organizaciones comparten la misma aplicación y la misma base de datos, sin poder ver los datos de las otras. Los usuarios crean solicitudes de soporte, un agente las atiende siguiendo una máquina de estados, y cada solicitud tiene un plazo de atención (SLA) que el servidor calcula según la categoría y la prioridad.

Prueba técnica para Sitecpro.

## Estado actual

- **Backend: completo.** Los 9 endpoints del contrato, las reglas RN-01 a RN-07 y 45 pruebas unitarias en verde.
- **Frontend (Vue 3): en desarrollo.** Todavía no está en el repositorio.

El detalle de qué hay y qué falta está al final de este archivo.

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

Nada más. La base de datos es SQLite (un archivo local que se crea solo), así que no hay que instalar ni configurar ningún motor de base de datos.

## Cómo levantar el backend

```bash
cd backend/src/Api
dotnet run
```

Eso es todo. Al arrancar, la aplicación aplica las migraciones y siembra los datos de prueba automáticamente si la base está vacía. Queda disponible:

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

## Estructura del backend

```
backend/
├─ src/
│  ├─ Api/              controllers, manejo de errores, Program.cs
│  ├─ Aplicacion/       contratos de servicios y DTOs
│  ├─ Dominio/          entidades, enums y reglas de negocio puras
│  └─ Infraestructura/  EF Core, migraciones, semilla, servicios, JWT
└─ tests/
   └─ Tests/            pruebas unitarias del dominio
```

La máquina de estados, el cálculo del SLA y los permisos por rol viven en `Dominio/Reglas` como clases estáticas puras, sin dependencia de EF ni de ASP.NET. Por eso se pueden probar sin levantar la aplicación.

## Qué está implementado y qué no

Implementado:

- Los 9 endpoints del contrato (`/auth/login`, `/me`, `/categorias`, CRUD de solicitudes, transiciones, `/health`).
- Aislamiento por tenant (RN-01): un recurso de otra organización responde 404, nunca 403.
- Máquina de estados (RN-02) implementada a mano, sin librerías.
- Permisos por rol (RN-03), cálculo y recálculo del SLA (RN-04), validación de agente al asignar (RN-05), motivos obligatorios al resolver/cancelar (RN-06) y código correlativo por organización y año (RN-07).
- Formato de error unificado `application/problem+json` con el campo `codigo` en todos los errores, incluidos los 401 del middleware JWT y los errores de validación automática.
- JWT HS256 con expiración de 8 horas, contraseñas con BCrypt, Swagger con esquema Bearer, CORS para `http://localhost:5173`, manejador global de excepciones.
- Migraciones y semilla automáticas al arrancar.

Pendiente:

- El frontend completo (Vue 3 + TypeScript). Es lo que sigue.
- `docker-compose.yml` (opcional en el enunciado).

Decisiones tomadas ante ambigüedades del enunciado (por ejemplo, en qué estados puede editar un Admin/Agente) están en `DECISIONES.md`.
