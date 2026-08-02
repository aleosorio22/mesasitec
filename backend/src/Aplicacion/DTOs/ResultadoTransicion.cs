namespace Mesasitec.Aplicacion.DTOs;

// Desenlaces posibles de ejecutar una transición. El controller mapea cada uno
// a su código HTTP y su "codigo" del contrato (§6.1).
public enum ResultadoTransicion
{
    Ok,                    // 200
    NoEncontrada,          // 404 RECURSO_NO_ENCONTRADO (no existe / otra org / ajena)
    NoPermitida,           // 403 OPERACION_NO_PERMITIDA (el rol no puede esa acción, RN-03)
    TransicionInvalida,    // 409 TRANSICION_INVALIDA (fuera de la máquina de estados, RN-02)
    AgenteInvalido,        // 422 AGENTE_INVALIDO (RN-05)
    MotivoRequerido,       // 422 MOTIVO_REQUERIDO (RN-06)
}