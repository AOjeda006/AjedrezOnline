using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que procesa el abandono de una sala (de forma explícita o por desconexión).
    /// </summary>
    public interface IAbandonarSalaUseCase
    {
        /// <summary>
        /// Localiza la sala del jugador, finaliza por abandono la partida si seguía en curso y
        /// elimina la sala.
        /// </summary>
        /// <param name="connectionId">Identificador de conexión SignalR del jugador que abandona.</param>
        /// <returns>
        /// Una tupla <c>(salaId, partidaId, resultado, ganadorId)</c>: el id de la sala abandonada;
        /// el id de la partida afectada (cadena vacía si no había); el <see cref="ResultadoPartida"/>
        /// derivado del abandono, o <c>null</c> si la partida no llegó a finalizarse; y el id del
        /// jugador ganador, o <c>null</c>. Si el jugador no estaba en ninguna sala, todos los
        /// elementos vienen vacíos o a <c>null</c>.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="connectionId"/> es nulo o está en blanco.</exception>
        Task<(string salaId, string partidaId, ResultadoPartida?, string?)> ExecuteAsync(string connectionId);
    }
}
