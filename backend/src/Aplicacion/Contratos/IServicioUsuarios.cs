using Mesasitec.Aplicacion.DTOs;

namespace Mesasitec.Aplicacion.Contratos;

public interface IServicioUsuarios
{
    // Agentes y admins ACTIVOS del tenant (los válidos para 'asignar' según RN-05).
    // Endpoint extra fuera del contrato: el frontend lo necesita para poblar
    // el select del modal de asignación. Declarado en DECISIONES.md.
    Task<IReadOnlyList<AgenteResumenDto>> ListarAgentesAsync(Guid tenantId);
}
