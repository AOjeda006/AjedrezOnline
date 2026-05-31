using ServidorAjedrez.Domain.Entities;

namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que une a un jugador a una sala existente e inicia la partida.
    /// </summary>
    public interface IUnirseSalaUseCase
    {
        /// <summary>
        /// Une al jugador a la sala indicada como oponente, crea la partida y la pone en curso.
        /// </summary>
        /// <param name="nombreSala">Nombre de la sala a la que unirse; no puede ser nulo ni estar en blanco.</param>
        /// <param name="nombreJugador">Nombre del jugador que se une; no puede ser nulo ni estar en blanco.</param>
        /// <param name="connectionId">Identificador de conexión SignalR del jugador; no puede ser nulo ni estar en blanco.</param>
        /// <returns>La <see cref="Partida"/> recién iniciada.</returns>
        /// <exception cref="ArgumentException">Alguno de los argumentos es nulo o está en blanco.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.SalaNoEncontradaException">No existe ninguna sala abierta con ese nombre.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.SalaCompletaException">La sala ya tiene a sus dos jugadores.</exception>
        Task<Partida> ExecuteAsync(string nombreSala, string nombreJugador, string connectionId);
    }
}
