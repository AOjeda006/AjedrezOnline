using ServidorAjedrez.Domain.Entities;

namespace ServidorAjedrez.Domain.DTOs
{
    /// <summary>
    /// Forma serializable de un <see cref="Tablero"/>: las piezas y el historial de movimientos.
    /// </summary>
    public class TableroDTO
    {
        public List<PiezaDTO>? Piezas { get; set; }
        public List<MovimientoDTO>? HistorialMovimientos { get; set; }

        /// <summary>Crea el DTO a partir de un tablero de dominio, proyectando sus piezas y su historial.</summary>
        /// <param name="tablero">Tablero de dominio a convertir.</param>
        /// <returns>El <see cref="TableroDTO"/> equivalente.</returns>
        public static TableroDTO FromDomain(Tablero tablero)
        {
            return new TableroDTO
            {
                Piezas = tablero.Piezas.Select(p => PiezaDTO.FromDomain(p)).ToList(),
                HistorialMovimientos = tablero.HistorialMovimientos.Select(m => MovimientoDTO.FromDomain(m)).ToList()
            };
        }
    }
}
