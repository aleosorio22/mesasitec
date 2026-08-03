using Mesasitec.Dominio.Entidades;
using Mesasitec.Dominio.Enums;
using Mesasitec.Dominio.Reglas;
namespace Mesasitec.Infraestructura.Data;


public static class DatosSemilla
{
    public static void Sembrar(MesaSitecDbContext db, DateTime fechaBase)
    {
        // Idempotencia: si ya hay usuarios, la base ya fue sembrada -> no hacer nada.
        if (db.Usuarios.Any())
            return;

        // Hasheamos la contraseña UNA vez y la reutilizamos para los 7 usuarios semilla.
        // En la base NUNCA se guarda "Sitec.2026", solo su hash BCrypt (irreversible).
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Sitec.2026");

        // --- 1. Tenants ---
        // Generamos los Guid a mano para poder enlazar los usuarios a su tenant.
        var norteId = Guid.NewGuid();
        var surId = Guid.NewGuid();

        var norte = new Tenant { Id = norteId, Nombre = "Cooperativa Norte", Activo = true };
        var sur   = new Tenant { Id = surId,   Nombre = "Bufete Sur",       Activo = true };

        db.Tenants.AddRange(norte, sur);

        // --- 2. Usuarios ---
        var usuarios = new List<Usuario>
        {
            // Cooperativa Norte: 1 admin, 2 agentes, 2 solicitantes
            new() { Id = Guid.NewGuid(), TenantId = norteId, Email = "admin@norte.test",
                    Nombre = "Admin Norte",   Rol = Rol.Admin,       PasswordHash = passwordHash, Activo = true },
            new() { Id = Guid.NewGuid(), TenantId = norteId, Email = "agente1@norte.test",
                    Nombre = "Agente Uno",    Rol = Rol.Agente,      PasswordHash = passwordHash, Activo = true },
            new() { Id = Guid.NewGuid(), TenantId = norteId, Email = "agente2@norte.test",
                    Nombre = "Agente Dos",    Rol = Rol.Agente,      PasswordHash = passwordHash, Activo = true },
            new() { Id = Guid.NewGuid(), TenantId = norteId, Email = "user1@norte.test",
                    Nombre = "Usuario Uno",   Rol = Rol.Solicitante, PasswordHash = passwordHash, Activo = true },
            new() { Id = Guid.NewGuid(), TenantId = norteId, Email = "user2@norte.test",
                    Nombre = "Usuario Dos",   Rol = Rol.Solicitante, PasswordHash = passwordHash, Activo = true },

            // Bufete Sur: 1 admin, 1 solicitante
            new() { Id = Guid.NewGuid(), TenantId = surId, Email = "admin@sur.test",
                    Nombre = "Admin Sur",     Rol = Rol.Admin,       PasswordHash = passwordHash, Activo = true },
            new() { Id = Guid.NewGuid(), TenantId = surId, Email = "user1@sur.test",
                    Nombre = "Usuario Sur",   Rol = Rol.Solicitante, PasswordHash = passwordHash, Activo = true },
        };

        db.Usuarios.AddRange(usuarios);

        // --- 3. Categorías (las mismas 4 en ambos tenants → 8 en total) ---
        // Función local que arma las 4 categorías de un tenant.
        // Devuelve un diccionario nombre -> Categoria para poder referenciarlas
        // por nombre al crear solicitudes en el siguiente tramo.
        Dictionary<string, Categoria> CrearCategorias(Guid tenantId)
        {
            var lista = new[]
            {
                new Categoria { Id = Guid.NewGuid(), TenantId = tenantId, Nombre = "Incidente",     SlaHoras = 8,  Activo = true },
                new Categoria { Id = Guid.NewGuid(), TenantId = tenantId, Nombre = "Requerimiento", SlaHoras = 40, Activo = true },
                new Categoria { Id = Guid.NewGuid(), TenantId = tenantId, Nombre = "Consulta",      SlaHoras = 24, Activo = true },
                new Categoria { Id = Guid.NewGuid(), TenantId = tenantId, Nombre = "Falla crítica", SlaHoras = 4,  Activo = true },
            };
            db.Categorias.AddRange(lista);
            return lista.ToDictionary(c => c.Nombre);
        }

        var categoriasNorte = CrearCategorias(norteId);
        var categoriasSur   = CrearCategorias(surId);

        // --- Referencias a usuarios (para enlazar solicitante/agente por variable) ---
        // .First(predicado) = LINQ, como .find() de JS pero lanza si no encuentra.
        var agente1Norte = usuarios.First(u => u.Email == "agente1@norte.test");
        var agente2Norte = usuarios.First(u => u.Email == "agente2@norte.test");
        var user1Norte   = usuarios.First(u => u.Email == "user1@norte.test");
        var user2Norte   = usuarios.First(u => u.Email == "user2@norte.test");
        var adminSur     = usuarios.First(u => u.Email == "admin@sur.test");
        var user1Sur     = usuarios.First(u => u.Email == "user1@sur.test");

        // Contadores de correlativo, INDEPENDIENTES por organización (RN-07).
        int correlativoNorte = 0;
        int correlativoSur = 0;

        // Fábrica: arma una Solicitud coherente a partir de pocos datos.
        // Se encarga sola del código, el SLA y los campos que dependen del estado.
        Solicitud Crear(
            Guid tenantId, ref int correlativo,
            Categoria categoria, Prioridad prioridad, Estado estado,
            Usuario solicitante, Usuario? agente,
            string titulo)
        {
            correlativo++;

            // La fecha se DERIVA del correlativo: a mayor correlativo, más reciente.
            // Cada solicitud se separa 6h de la anterior. La #1 es la más vieja
            // (queda 29×6h = 174h ANTES de fechaBase; la #30 caería en fechaBase).
            // Todo es offset FIJO respecto a fechaBase → datos reproducibles (§6.3).
            var horasAntesDeBase = (30 - correlativo) * 6.0;
            var fechaCreacion = fechaBase.AddHours(-horasAntesDeBase);

            var codigo = $"SOL-{fechaCreacion.Year}-{correlativo:D5}";
            var fechaLimite = CalculadoraSla.CalcularFechaLimite(
                fechaCreacion, categoria.SlaHoras, prioridad);

            var s = new Solicitud
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Codigo = codigo,
                Titulo = titulo,
                Descripcion = $"{titulo}. Caso registrado en la mesa de servicio para su atención.",
                CategoriaId = categoria.Id,
                Prioridad = prioridad,
                Estado = estado,
                SolicitanteId = solicitante.Id,
                AgenteId = agente?.Id,
                FechaCreacion = fechaCreacion,
                FechaLimiteSla = fechaLimite,
            };

            if (estado is Estado.Resuelta or Estado.Cerrada)
            {
                s.FechaResolucion = fechaCreacion.AddHours(2);
                s.MotivoResolucion = "Se atendió la solicitud y se validó la solución con el usuario final.";
            }
            else if (estado == Estado.Cancelada)
            {
                s.MotivoCancelacion = "Solicitud cancelada por gestión interna.";
            }

            return s;
        }

        var solicitudes = new List<Solicitud>();

        // ================= COOPERATIVA NORTE (25) =================
        // --- Nuevas (5): activas, sin agente ---
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Incidente"],     Prioridad.Alta,    Estado.Nueva, user1Norte, null, "No puedo acceder al portal de clientes"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Consulta"],      Prioridad.Baja,    Estado.Nueva, user2Norte, null,  "Consulta sobre horarios de atención"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Requerimiento"], Prioridad.Media,   Estado.Nueva, user1Norte, null,  "Solicito acceso al módulo de reportes"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Falla crítica"], Prioridad.Critica, Estado.Nueva, user2Norte, null,  "El sistema de facturación está caído"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Incidente"],     Prioridad.Media,   Estado.Nueva, user1Norte, null,  "Error al generar comprobante de pago"));

        // --- Asignadas (4): con agente ---
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Incidente"],     Prioridad.Alta,    Estado.Asignada, user1Norte, agente1Norte, "Correo corporativo no envía adjuntos"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Falla crítica"], Prioridad.Critica, Estado.Asignada, user2Norte, agente2Norte, "Caída intermitente de la red interna"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Requerimiento"], Prioridad.Baja,    Estado.Asignada, user1Norte, agente1Norte, "Actualización de datos de contacto"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Consulta"],      Prioridad.Media,   Estado.Asignada, user2Norte, agente2Norte, "Duda sobre proceso de aprobación"));

        // --- En proceso (4): con agente ---
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Incidente"],     Prioridad.Alta,    Estado.EnProceso, user1Norte, agente1Norte, "Impresora de red no responde"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Falla crítica"], Prioridad.Critica, Estado.EnProceso, user2Norte, agente2Norte, "Base de datos con lentitud extrema"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Requerimiento"], Prioridad.Media,   Estado.EnProceso, user1Norte, agente1Norte, "Instalación de software de diseño"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Consulta"],      Prioridad.Baja,    Estado.EnProceso, user2Norte, agente2Norte, "Información sobre política de respaldos"));

        // --- Resueltas (4): con agente + fecha/motivo de resolución ---
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Incidente"],     Prioridad.Media,   Estado.Resuelta, user1Norte, agente1Norte, "Restablecimiento de contraseña"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Consulta"],      Prioridad.Baja,    Estado.Resuelta, user2Norte, agente2Norte, "Consulta sobre vacaciones atendida"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Falla crítica"], Prioridad.Alta,    Estado.Resuelta, user1Norte, agente1Norte, "Servicio web restaurado"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Requerimiento"], Prioridad.Media,   Estado.Resuelta, user2Norte, agente2Norte, "Alta de nuevo usuario completada"));

        // --- Cerradas (4): con agente + resolución (ya finalizadas) ---
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Incidente"],     Prioridad.Baja,    Estado.Cerrada, user1Norte, agente1Norte, "Ajuste menor en configuración"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Consulta"],      Prioridad.Media,   Estado.Cerrada, user2Norte, agente2Norte, "Consulta atendida y cerrada"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Requerimiento"], Prioridad.Alta,    Estado.Cerrada, user1Norte, agente1Norte, "Entrega de equipo completada"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Incidente"],     Prioridad.Critica, Estado.Cerrada, user2Norte, agente2Norte, "Incidente crítico resuelto y cerrado"));

        // --- Canceladas (4): con motivo de cancelación, sin agente ---
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Consulta"],      Prioridad.Baja,    Estado.Cancelada, user1Norte, null, "Consulta cancelada por el usuario"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Requerimiento"], Prioridad.Media,   Estado.Cancelada, user2Norte, null, "Requerimiento duplicado"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Incidente"],     Prioridad.Alta,    Estado.Cancelada, user1Norte, null, "Reporte cancelado por error del usuario"));
        solicitudes.Add(Crear(norteId, ref correlativoNorte, categoriasNorte["Falla crítica"], Prioridad.Critica, Estado.Cancelada, user2Norte, null, "Falla reportada por equivocación"));

        // ================= BUFETE SUR (8) =================
        // Nota: Sur no tiene rol Agente; su Admin actúa como agente (RN-05 permite Agente O Admin).
        solicitudes.Add(Crear(surId, ref correlativoSur, categoriasSur["Incidente"],     Prioridad.Alta,    Estado.Nueva,     user1Sur, null,  "No carga el expediente digital"));
        solicitudes.Add(Crear(surId, ref correlativoSur, categoriasSur["Consulta"],      Prioridad.Media,   Estado.Nueva,     user1Sur, null, "Consulta sobre firma electrónica"));
        solicitudes.Add(Crear(surId, ref correlativoSur, categoriasSur["Requerimiento"], Prioridad.Baja,    Estado.Asignada,  user1Sur, adminSur, "Solicitud de nueva credencial"));
        solicitudes.Add(Crear(surId, ref correlativoSur, categoriasSur["Falla crítica"], Prioridad.Critica, Estado.EnProceso, user1Sur, adminSur,  "Sistema de gestión de casos caído"));
        solicitudes.Add(Crear(surId, ref correlativoSur, categoriasSur["Incidente"],     Prioridad.Media,   Estado.Resuelta,  user1Sur, adminSur, "Acceso restablecido"));
        solicitudes.Add(Crear(surId, ref correlativoSur, categoriasSur["Consulta"],      Prioridad.Baja,    Estado.Resuelta,  user1Sur, adminSur, "Consulta legal atendida"));
        solicitudes.Add(Crear(surId, ref correlativoSur, categoriasSur["Requerimiento"], Prioridad.Alta,    Estado.Cerrada,   user1Sur, adminSur, "Documentación entregada"));
        solicitudes.Add(Crear(surId, ref correlativoSur, categoriasSur["Consulta"],      Prioridad.Media,   Estado.Cancelada, user1Sur, null,     "Consulta cancelada"));

        db.Solicitudes.AddRange(solicitudes);


        db.SaveChanges();
    }
}