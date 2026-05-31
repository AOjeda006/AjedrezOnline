using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.DTOs
{
    /// <summary>
    /// Forma serializable de un <see cref="Movimiento"/>. El cliente la envía al mover y el servidor
    /// la emite al notificar el movimiento realizado.
    /// </summary>
    /// <remarks>
    /// Las propiedades son anulables para tolerar campos ausentes al deserializar; el mapeo a
    /// dominio (<see cref="ServidorAjedrez.Domain.UseCases.Mappers.MovimientoMapper"/>) valida los
    /// imprescindibles.
    /// </remarks>
    public class MovimientoDTO
    {
        public string? Id { get; set; }
        public string? PiezaId { get; set; }
        public PosicionDTO? Origen { get; set; }
        public PosicionDTO? Destino { get; set; }
        public string? PiezaCapturada { get; set; }
        public bool? EsEnroque { get; set; }
        public bool? EsPromocion { get; set; }

        /// <summary>Crea el DTO a partir de un movimiento de dominio.</summary>
        /// <param name="movimiento">Movimiento de dominio a convertir.</param>
        /// <returns>El <see cref="MovimientoDTO"/> equivalente.</returns>
        public static MovimientoDTO FromDomain(Movimiento movimiento)
        {
            return new MovimientoDTO
            {
                Id = movimiento.Id,
                PiezaId = movimiento.PiezaId,
                Origen = PosicionDTO.FromDomain(movimiento.Origen),
                Destino = PosicionDTO.FromDomain(movimiento.Destino),
                PiezaCapturada = movimiento.PiezaCapturada,
                EsEnroque = movimiento.EsEnroque,
                EsPromocion = movimiento.EsPromocion
            };
        }
    }
}
