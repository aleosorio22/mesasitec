using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mesasitec.Aplicacion.Contratos;
using Mesasitec.Dominio.Entidades;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Mesasitec.Infraestructura.Seguridad;

// Implementa IGeneradorTokens usando la librería JWT de .NET.
// Lee el secreto/issuer/audience de la configuración (que a su vez puede venir
// de appsettings.json o de variables de entorno).
public class GeneradorTokens : IGeneradorTokens
{
    private readonly IConfiguration _config;

    // El DbContext no hace falta aquí; solo la configuración. Se inyecta.
    public GeneradorTokens(IConfiguration config)
    {
        _config = config;
    }

    public (string token, int expiraEnSegundos) Generar(Usuario usuario)
    {
        // 1. Lee la config del JWT (el secreto puede venir de variable de entorno vía JWT__SECRET).
        var secret   = _config["Jwt:Secret"]!;
        var issuer   = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];

        // 2. Los 4 claims que exige el enunciado (§5.1).
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new("tenantId", usuario.TenantId.ToString()),
            new("rol", usuario.Rol.ToString()),
            new("email", usuario.Email),
        };

        // 3. La credencial de firma: secreto -> bytes -> clave -> firma HS256.
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        // 4. Expiración: 8 horas (§5.1).
        const int expiraEnSegundos = 8 * 60 * 60; // 28800
        var expira = DateTime.UtcNow.AddSeconds(expiraEnSegundos);

        // 5. Arma el token con todo lo anterior.
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        // 6. Serializa el token a su forma de cadena (eyJ...).
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenString, expiraEnSegundos);
    }
}