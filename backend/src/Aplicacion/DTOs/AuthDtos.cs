namespace Mesasitec.Aplicacion.DTOs;

// Petición del login (lo que llega en el body del POST).
public record LoginRequest(string Email, string Password);

// El objeto "usuario" que devuelve el login y también /me (§6.2).
public record UsuarioDto(
    Guid Id,
    string Nombre,
    string Email,
    string Rol,
    Guid TenantId,
    string TenantNombre);

// La respuesta completa del login.
public record LoginResponse(
    string AccessToken,
    int ExpiraEn,
    UsuarioDto Usuario);