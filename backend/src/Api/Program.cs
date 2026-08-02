using Mesasitec.Infraestructura.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    });

// Registra nuestro generador de tokens para poder inyectarlo (interfaz -> implementación).
builder.Services.AddScoped<IGeneradorTokens, GeneradorTokens>();

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();
