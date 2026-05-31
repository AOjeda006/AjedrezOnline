using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.DTOs
{
    /// <summary>
    /// Forma serializable de una <see cref="Pieza"/> para enviarla al cliente dentro del tablero.
    /// </summary>
    public class PiezaDTO
    {
        public string? Id { get; set; }
        public TipoPieza? Tipo { get; set; }
        public Color? Color { get; set; }
        public PosicionDTO? Posicion { get; set; }
        public bool? Eliminada { get; set; }

        /// <summary>Crea el DTO a partir de una pieza de dominio.</summary>
        /// <param name="pieza">Pieza de dominio a convertir.</param>
        /// <returns>El <see cref="PiezaDTO"/> equivalente.</returns>
        public static PiezaDTO FromDomain(Pieza pieza)
        {
            return new PiezaDTO
            {
                Id = pieza.Id,
                Tipo = pieza.Tipo,
                Color = pieza.Color,
                Posicion = PosicionDTO.FromDomain(pieza.Posicion),
                Eliminada = pieza.Eliminada
            };
        }
    }
}
