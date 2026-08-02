using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Aplicacion.DTOs;
using Mesasitec.Infraestructura.Data;
using Microsoft.EntityFrameworkCore;

namespace Mesasitec.Infraestructura.Seguridad;

public class ServicioAuth : IServicioAuth
{
    private readonly MesaSitecDbContext _db;
    private readonly IGeneradorTokens _generador;

    // Se inyectan la base de datos y el generador de tokens.
    public ServicioAuth(MesaSitecDbContext db, IGeneradorTokens generador)
    {
        _db = db;
        _generador = generador;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // 1. Busca el usuario por email, trayendo también su Tenant (para el nombre).
        //    Include = "trae también la relación" (como un JOIN). Es el "eager loading" de EF Core.
        var usuario = await _db.Usuarios
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        // 2. Si no existe el usuario, o está inactivo, o la contraseña no coincide -> null.
        //    Un solo return null para los 3 casos: no revelamos CUÁL falló (buena práctica de seguridad).
        if (usuario is null || !usuario.Activo)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            return null;

        // 3. Credenciales válidas: genera el token.
        var (token, expiraEn) = _generador.Generar(usuario);

        // 4. Arma el DTO de respuesta (SIN el passwordHash, claro).
        var usuarioDto = new UsuarioDto(
            usuario.Id,
            usuario.Nombre,
            usuario.Email,
            usuario.Rol.ToString(),          // enum -> "Agente"
            usuario.TenantId,
            usuario.Tenant!.Nombre);         // el nombre del tenant que trajimos con Include

        return new LoginResponse(token, expiraEn, usuarioDto);
    }
}