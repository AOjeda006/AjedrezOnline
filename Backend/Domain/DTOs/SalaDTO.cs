using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.DTOs
{
    /// <summary>
    /// Forma serializable de una <see cref="Sala"/> para enviarla al cliente.
    /// </summary>
    public class SalaDTO
    {
        public string? Id { get; set; }
        public string? Nombre { get; set; }
        public JugadorDTO? Creador { get; set; }
        public JugadorDTO? Oponente { get; set; }
        public EstadoSala? Estado { get; set; }

        /// <summary>Crea el DTO a partir de una sala de dominio.</summary>
        /// <param name="sala">Sala de dominio a convertir.</param>
        /// <returns>El <see cref="SalaDTO"/> equivalente.</returns>
        public static SalaDTO FromDomain(Sala sala)
        {
            return new SalaDTO
            {
                Id = sala.Id,
                Nombre = sala.Nombre,
                Creador = JugadorDTO.FromDomain(sala.Creador),
                Oponente = sala.Oponente != null ? JugadorDTO.FromDomain(sala.Oponente) : null,
                Estado = sala.Estado
            };
        }
    }
}
