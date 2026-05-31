using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.DTOs
{
    /// <summary>
    /// Forma serializable de una <see cref="ServidorAjedrez.Domain.ValueObjects.Posicion"/> para
    /// intercambiarla con el cliente. Las propiedades son anulables para tolerar campos ausentes
    /// al deserializar lo que envía el cliente.
    /// </summary>
    public class PosicionDTO
    {
        public int? Fila { get; set; }
        public int? Columna { get; set; }

        /// <summary>Crea el DTO a partir de una posición de dominio.</summary>
        /// <param name="posicion">Posición de dominio a convertir.</param>
        /// <returns>El <see cref="PosicionDTO"/> equivalente.</returns>
        public static PosicionDTO FromDomain(ServidorAjedrez.Domain.ValueObjects.Posicion posicion)
        {
            return new PosicionDTO
            {
                Fila = posicion.Fila,
                Columna = posicion.Columna
            };
        }
    }
}
