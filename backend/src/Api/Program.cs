using Mesasitec.Infraestructura.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Mesasitec.Infraestructura.Servicios;
using Mesasitec.Api.Errores;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serializa/deserializa los enums por su NOMBRE ("Nueva") en vez de por número (0).
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(
            new Mesasitec.Api.Serializacion.DateTimeUtcConverter());
        options.JsonSerializerOptions.Converters.Add(
            new Mesasitec.Api.Serializacion.DateTimeUtcNullableConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // §6.1: los errores de validación automática deben usar NUESTRO formato
        // (codigo + errores), no el ValidationProblemDetails default de ASP.NET.
        //  - Cuerpo inválido (POST/PUT): 422 VALIDACION con el diccionario 'errores'.
        //  - Query inválida (GET, ej. ?estado=foo): 400 PARAMETRO_INVALIDO.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errores = context.ModelState
                .Where(e => e.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    e => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(e.Key.TrimStart('$', '.')),
                    e => e.Value!.Errors
                        .Select(err => string.IsNullOrWhiteSpace(err.ErrorMessage)
                            ? "Valor inválido."
                            : err.ErrorMessage)
                        .ToArray());

            var error = HttpMethods.IsGet(context.HttpContext.Request.Method)
                ? ErroresApi.ParametroInvalido("Uno o más parámetros de consulta son inválidos.")
                : ErroresApi.Validacion("Uno o más campos son inválidos.", errores);

            return new ObjectResult(error)
            {
                StatusCode = error.Status,
                ContentTypes = { "application/problem+json" },
            };
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Define el esquema de seguridad "Bearer" (§5.1): un candado donde pegar el JWT.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega aquí el accessToken del login (sin escribir 'Bearer').",
    });

    // Aplica ese esquema a los endpoints, para que Swagger mande el token automáticamente.
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


var stringConnection = builder.Configuration.GetConnectionString("Default") ?? "Data Source=mesasitec.db";

builder.Services.AddDbContext<MesaSitecDbContext>(options => options.UseSqlite(stringConnection));

// --- Autenticación JWT ---
var jwtSecret   = builder.Configuration["Jwt:Secret"]!;
var jwtIssuer   = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Verifica que el token venga firmado con NUESTRO secreto.
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

            // Verifica que el emisor y la audiencia coincidan con los nuestros.
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,

            // Verifica que no esté expirado (sin margen de gracia extra).
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // §6.1: TODOS los errores llevan problem+json con 'codigo'.
        // Por defecto el middleware JWT responde 401/403 con cuerpo VACÍO; aquí lo reemplazamos.
        options.Events = new JwtBearerEvents
        {
            // Token ausente, inválido o expirado -> 401 NO_AUTENTICADO.
            OnChallenge = async context =>
            {
                context.HandleResponse(); // suprime la respuesta vacía default
                var error = ErroresApi.NoAutenticado("Token ausente, inválido o expirado.");
                context.Response.StatusCode = error.Status;
                // El contentType va en la llamada: WriteAsJsonAsync pisa Response.ContentType.
                await context.Response.WriteAsJsonAsync(
                    error, options: null, contentType: "application/problem+json");
            },
            // 403 emitido por el middleware de autorización (hoy los permisos se
            // resuelven en los servicios, pero así el contrato queda cubierto).
            OnForbidden = async context =>
            {
                var error = ErroresApi.OperacionNoPermitida();
                context.Response.StatusCode = error.Status;
                await context.Response.WriteAsJsonAsync(
                    error, options: null, contentType: "application/problem+json");
            },
        };
    });

// CORS (§5.1): el frontend Vue corre en http://localhost:5173 y el navegador
// bloquea sus llamadas a la API si este origen no está permitido explícitamente.
const string CorsFrontend = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsFrontend, policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddScoped<IGeneradorTokens, GeneradorTokens>();
builder.Services.AddScoped<IServicioAuth, ServicioAuth>();
builder.Services.AddScoped<IServicioSolicitudes, ServicioSolicitudes>();
builder.Services.AddScoped<IServicioCategorias, ServicioCategorias>();
// Manejador global de excepciones (§5.3).
builder.Services.AddExceptionHandler<ManejadorExcepcionesGlobal>();
builder.Services.AddProblemDetails();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MesaSitecDbContext>();

    // 1. Aplica migraciones pendientes (crea el .db la 1.ª vez).
    db.Database.Migrate();

    // 2. Lee SEED_FECHA_BASE de variable de entorno; si no está, usa el default del enunciado.
    var seedFechaRaw = Environment.GetEnvironmentVariable("SEED_FECHA_BASE")
        ?? "2026-01-15T08:00:00Z";

    // Parseo a UTC. AdjustToUniversal garantiza DateTimeKind.Utc (para que salga con "Z" en el JSON).
    var fechaBase = DateTime.Parse(
        seedFechaRaw,
        System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.AdjustToUniversal);

    // 3. Siembra la base si está vacía.
    DatosSemilla.Sembrar(db, fechaBase);
}

// Configure the HTTP request pipeline.
// Swagger SIEMPRE activo (checklist §11 exige /swagger accesible, sin importar el entorno).
app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionHandler();
app.UseCors(CorsFrontend);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
