using Mesasitec.Dominio.Entidades;

namespace Mesasitec.Aplicacion.Contratos;

// Contrato: "algo que sabe generar un JWT para un usuario".
// Aplicacion define QUÉ necesita; Infraestructura implementa el CÓMO.
public interface IGeneradorTokens
{
    // Devuelve el token firmado y cuántos segundos dura (para el campo expiraEn del login).
    (string token, int expiraEnSegundos) Generar(Usuario usuario);
}