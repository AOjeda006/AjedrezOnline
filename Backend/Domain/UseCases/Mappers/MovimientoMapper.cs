using ServidorAjedrez.Domain.DTOs;
using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.ValueObjects;

namespace ServidorAjedrez.Domain.UseCases.Mappers
{
    /// <summary>
    /// Conversión del <see cref="MovimientoDTO"/> recibido del cliente a la entidad de dominio
    /// <see cref="Movimiento"/>.
    /// </summary>
    public static class MovimientoMapper
    {
        /// <summary>
        /// Construye un <see cref="Movimiento"/> de dominio a partir del DTO, validando los campos
        /// imprescindibles. Las coordenadas nulas se interpretan como 0.
        /// </summary>
        /// <param name="dto">DTO con los datos del movimiento recibidos del cliente.</param>
        /// <returns>El <see cref="Movimiento"/> equivalente, con posiciones de origen y destino validadas.</returns>
        /// <exception cref="ArgumentException">
        /// Falta <see cref="MovimientoDTO.PiezaId"/>; <see cref="MovimientoDTO.Origen"/> o
        /// <see cref="MovimientoDTO.Destino"/> son nulos; o las posiciones resultantes caen fuera del tablero.
        /// </exception>
        public static Movimiento FromDTO(MovimientoDTO dto)
        {
            if (string.IsNullOrEmpty(dto.PiezaId))
                throw new ArgumentException("PiezaId es requerido");
            if (dto.Origen == null || dto.Destino == null)
                throw new ArgumentException("Origen y Destino son requeridos");

            var origen = Posicion.Create(dto.Origen.Fila ?? 0, dto.Origen.Columna ?? 0);
            var destino = Posicion.Create(dto.Destino.Fila ?? 0, dto.Destino.Columna ?? 0);

            return Movimiento.Create(
                dto.PiezaId,
                origen,
                destino,
                dto.PiezaCapturada,
                dto.EsEnroque ?? false,
                dto.EsPromocion ?? false
            );
        }
    }
}
