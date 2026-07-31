namespace Mesasitec.Dominio.Enums;

// El vocabulario del flujo. La API recibe strings ("asignar", "iniciar"...)
// y los parsea a este enum antes de tocar el dominio.
public enum Accion
{
    Asignar,
    Iniciar,
    Resolver,
    Cerrar,
    Reabrir,
    Cancelar
}