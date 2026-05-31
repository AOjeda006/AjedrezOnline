using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.DTOs
{
    /// <summary>
    /// Forma serializable de un <see cref="Jugador"/> para enviarlo al cliente.
    /// </summary>
    public class JugadorDTO
    {
        public string? Id { get; set; }
        public string? Nombre { get; set; }
        public string? ConnectionId { get; set; }
        public Color? Color { get; set; }

        /// <summary>Crea el DTO a partir de un jugador de dominio.</summary>
        /// <param name="jugador">Jugador de dominio a convertir.</param>
        /// <returns>El <see cref="JugadorDTO"/> equivalente.</returns>
        public static JugadorDTO FromDomain(Jugador jugador)
        {
            return new JugadorDTO
            {
                Id = jugador.Id,
                Nombre = jugador.Nombre,
                ConnectionId = jugador.ConnectionId,
                Color = jugador.Color
            };
        }
    }
}
