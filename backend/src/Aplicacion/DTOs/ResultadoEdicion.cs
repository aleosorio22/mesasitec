namespace Mesasitec.Aplicacion.DTOs;

// Los posibles desenlaces de editar una solicitud. Permite al controller
// traducir cada caso a su código HTTP correcto (404 / 409 / 422 / 200).
public enum ResultadoEdicion
{
    Ok,
    NoEncontrada,       // no existe, otra org, o ajena de un solicitante -> 404
    EstadoNoEditable,   // no está en Nueva/Asignada (RN-08) -> 409
    CategoriaInvalida,  // categoría inexistente o de otra org -> 422
}