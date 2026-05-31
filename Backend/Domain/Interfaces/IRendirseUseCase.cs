using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que registra la rendición de un jugador y finaliza la partida.
    /// </summary>
    public interface IRendirseUseCase
    {
        /// <summary>Finaliza la partida por rendición (victoria del rival) y cierra la sala asociada.</summary>
        /// <param name="partidaId">Id de la partida; no puede ser nulo ni estar en blanco.</param>
        /// <param name="jugadorId">Identificador de conexión SignalR del jugador que se rinde; se resuelve al jugador de dominio.</param>
        /// <returns>Una tupla con el <see cref="ResultadoPartida"/> final y el id de dominio del jugador que se rinde.</returns>
        /// <exception cref="ArgumentException">Alguno de los argumentos es nulo o está en blanco.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.PartidaNoEncontradaException">No existe la partida indicada.</exception>
        Task<(ResultadoPartida, string)> ExecuteAsync(string partidaId, string jugadorId);
    }
}
