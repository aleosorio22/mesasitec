using Mesasitec.Dominio.Enums;

namespace Mesasitec.Dominio.Reglas;

// RN-02. Implementada a mano (el enunciado prefiere que no usemos librería).
// Un diccionario (estado, accion) -> estado destino: una sola fuente de verdad,
// trivial de extender si en la entrevista piden agregar un estado o transición.
public static class MaquinaEstados
{
    private static readonly IReadOnlyDictionary<(Estado, Accion), Estado> Transiciones =
        new Dictionary<(Estado, Accion), Estado>
        {
            { (Estado.Nueva,     Accion.Asignar),  Estado.Asignada  },
            { (Estado.Nueva,     Accion.Cancelar), Estado.Cancelada },

            { (Estado.Asignada,  Accion.Iniciar),  Estado.EnProceso },
            { (Estado.Asignada,  Accion.Asignar),  Estado.Asignada  }, // reasignar
            { (Estado.Asignada,  Accion.Cancelar), Estado.Cancelada },

            { (Estado.EnProceso, Accion.Resolver), Estado.Resuelta  },
            { (Estado.EnProceso, Accion.Asignar),  Estado.Asignada  }, // reasignar
            { (Estado.EnProceso, Accion.Cancelar), Estado.Cancelada },

            { (Estado.Resuelta,  Accion.Cerrar),   Estado.Cerrada   },
            { (Estado.Resuelta,  Accion.Reabrir),  Estado.EnProceso },
            // Cerrada y Cancelada son finales: no tienen entradas -> nada es válido.
        };

    public static bool EsTransicionValida(Estado actual, Accion accion)
        => Transiciones.ContainsKey((actual, accion));

    /// Devuelve true y el estado destino si la transición es válida; false en caso contrario.
    /// Puro y testeable, sin lanzar excepciones (la capa Aplicacion mapea el 409 TRANSICION_INVALIDA).
    public static bool TryAplicar(Estado actual, Accion accion, out Estado destino)
        => Transiciones.TryGetValue((actual, accion), out destino);

    /// Acciones disponibles desde un estado (útil para saber qué botones existen en el frontend).
    public static IReadOnlyCollection<Accion> AccionesPermitidas(Estado actual)
        => Transiciones.Keys
            .Where(k => k.Item1 == actual)
            .Select(k => k.Item2)
            .ToArray();
}