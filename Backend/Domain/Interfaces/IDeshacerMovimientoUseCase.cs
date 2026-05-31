using ServidorAjedrez.Domain.Entities;

namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que deshace el movimiento pendiente (sin confirmar) de una partida.
    /// </summary>
    public interface IDeshacerMovimientoUseCase
    {
        /// <summary>Revierte el movimiento pendiente y devuelve el tablero restaurado.</summary>
        /// <param name="partidaId">Id de la partida; no puede ser nulo ni estar en blanco.</param>
        /// <param name="jugadorId">Identificador de conexión SignalR del jugador que deshace; se resuelve al jugador de dominio.</param>
        /// <returns>El <see cref="Tablero"/> tras deshacer el movimiento.</returns>
        /// <exception cref="ArgumentException">Alguno de los argumentos es nulo o está en blanco.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.PartidaNoEncontradaException">No existe la partida indicada.</exception>
        /// <exception cref="InvalidOperationException">No hay movimiento pendiente, o no es el turno del jugador.</exception>
        Task<Tablero> ExecuteAsync(string partidaId, string jugadorId);
    }
}
