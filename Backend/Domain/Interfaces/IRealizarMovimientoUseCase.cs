using ServidorAjedrez.Domain.DTOs;
using ServidorAjedrez.Domain.Entities;

namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que aplica de forma tentativa un movimiento sobre una partida.
    /// </summary>
    public interface IRealizarMovimientoUseCase
    {
        /// <summary>
        /// Valida y aplica el movimiento descrito en el DTO, dejándolo pendiente de confirmación.
        /// </summary>
        /// <param name="partidaId">Id de la partida; no puede ser nulo ni estar en blanco.</param>
        /// <param name="movimientoDTO">Movimiento recibido del cliente; no puede ser nulo.</param>
        /// <param name="jugadorId">Identificador de conexión SignalR del jugador que mueve; el caso de uso lo resuelve al jugador de dominio.</param>
        /// <returns>Una tupla con el <see cref="Movimiento"/> aplicado y el <see cref="Tablero"/> resultante.</returns>
        /// <exception cref="ArgumentException"><paramref name="partidaId"/> o <paramref name="jugadorId"/> son nulos o están en blanco.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="movimientoDTO"/> es nulo.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.PartidaNoEncontradaException">No existe la partida indicada.</exception>
        /// <exception cref="InvalidOperationException">El movimiento es ilegal o no es el turno del jugador.</exception>
        Task<(Movimiento, Tablero)> ExecuteAsync(string partidaId, MovimientoDTO movimientoDTO, string jugadorId);
    }
}
