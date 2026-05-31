using ServidorAjedrez.Domain.Enums;
using ServidorAjedrez.Domain.Entities;

namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que corona el peón del movimiento pendiente y reevalúa el fin de la partida.
    /// </summary>
    public interface IPromocionarPeonUseCase
    {
        /// <summary>
        /// Promociona el peón al tipo indicado y comprueba si la coronación provoca jaque mate.
        /// </summary>
        /// <param name="partidaId">Id de la partida; no puede ser nulo ni estar en blanco.</param>
        /// <param name="tipo">Tipo al que se corona el peón (torre, caballo, alfil o reina).</param>
        /// <param name="jugadorId">Identificador de conexión SignalR del jugador que promociona; se resuelve al jugador de dominio.</param>
        /// <returns>
        /// Una tupla <c>(tablero, hayJaqueMate, resultado, ganadorId)</c>: el tablero tras la
        /// coronación; si esta provoca jaque mate; el <see cref="ResultadoPartida"/> final (o
        /// <c>null</c> si la partida sigue); y el id del jugador ganador (o <c>null</c>).
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="partidaId"/> o <paramref name="jugadorId"/> son nulos o están en blanco.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.PartidaNoEncontradaException">No existe la partida indicada.</exception>
        /// <exception cref="InvalidOperationException">No hay movimiento pendiente que promocionar, o el tipo de destino no es válido.</exception>
        Task<(Tablero, bool, ResultadoPartida?, string?)> ExecuteAsync(string partidaId, TipoPieza tipo, string jugadorId);
    }
}
