using Mesasitec.Infraestructura.Data;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var stringConnection = builder.Configuration.GetConnectionString("Default") ?? "Data Source=mesasitec.db";

builder.Services.AddDbContext<MesaSitecDbContext>(options => options.UseSqlite(stringConnection));

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

app.UseAuthorization();

app.MapControllers();

app.Run();
