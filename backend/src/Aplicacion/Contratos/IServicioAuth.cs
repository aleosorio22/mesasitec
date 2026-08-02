using Mesasitec.Aplicacion.DTOs;

namespace Mesasitec.Aplicacion.Contratos;

public interface IServicioAuth
{
    // Devuelve el LoginResponse si las credenciales son válidas, o null si no lo son.
    // (null = credenciales incorrectas; el controller lo traduce a 401 NO_AUTENTICADO.)
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<UsuarioDto?> ObtenerPerfilAsync(Guid usuarioId);
}