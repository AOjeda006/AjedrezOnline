using ServidorAjedrez.Domain.Entities;

namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que crea una sala nueva con un jugador como creador.
    /// </summary>
    public interface ICrearSalaUseCase
    {
        /// <summary>
        /// Crea una sala y registra a quien la solicita como creador, a la espera de un oponente.
        /// </summary>
        /// <param name="nombreSala">Nombre de la sala a crear; no puede ser nulo ni estar en blanco.</param>
        /// <param name="nombreJugador">Nombre del jugador creador; no puede ser nulo ni estar en blanco.</param>
        /// <param name="connectionId">Identificador de conexión SignalR del creador; no puede ser nulo ni estar en blanco.</param>
        /// <returns>La <see cref="Sala"/> creada.</returns>
        /// <exception cref="ArgumentException">Alguno de los argumentos es nulo o está en blanco.</exception>
        Task<Sala> ExecuteAsync(string nombreSala, string nombreJugador, string connectionId);
    }
}
