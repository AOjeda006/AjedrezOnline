using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que confirma el movimiento pendiente de una partida y consolida el turno.
    /// </summary>
    public interface IConfirmarMovimientoUseCase
    {
        /// <summary>
        /// Confirma el movimiento pendiente: cambia el turno, evalúa el jaque mate y señala si el
        /// movimiento requiere promoción.
        /// </summary>
        /// <param name="partidaId">Id de la partida; no puede ser nulo ni estar en blanco.</param>
        /// <param name="jugadorId">Identificador de conexión SignalR del jugador que confirma; se resuelve al jugador de dominio.</param>
        /// <returns>
        /// Una tupla <c>(turnoActual, numeroTurnos, hayJaqueMate, esPromocion, resultado, ganadorId)</c>:
        /// el color al que pasa el turno; el número de movimientos jugados; si la jugada da jaque
        /// mate; si el movimiento exige promoción de peón; el <see cref="ResultadoPartida"/> final
        /// (o <c>null</c> si la partida sigue); y el id del jugador ganador (o <c>null</c>).
        /// </returns>
        /// <exception cref="ArgumentException">Alguno de los argumentos es nulo o está en blanco.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.PartidaNoEncontradaException">No existe la partida indicada.</exception>
        /// <exception cref="InvalidOperationException">No hay movimiento pendiente, o no es el turno del jugador.</exception>
        Task<(Color, int, bool, bool, ResultadoPartida?, string?)> ExecuteAsync(string partidaId, string jugadorId);
    }
}
