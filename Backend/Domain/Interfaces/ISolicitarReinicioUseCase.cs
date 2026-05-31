using ServidorAjedrez.Domain.Entities;

namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que registra la solicitud de revancha de un jugador.
    /// </summary>
    public interface ISolicitarReinicioUseCase
    {
        /// <summary>
        /// Anota la solicitud de revancha; si ambos jugadores la han pedido, reinicia la partida.
        /// </summary>
        /// <param name="partidaId">Id de la partida; no puede ser nulo ni estar en blanco.</param>
        /// <param name="jugadorId">Identificador de conexión SignalR del jugador que solicita revancha; se resuelve al jugador de dominio.</param>
        /// <returns>
        /// Una tupla <c>(reinicioBlancas, reinicioNegras, partida)</c> con el estado de la solicitud
        /// de cada bando y la <see cref="Partida"/> ya reiniciada cuando ambos aceptan, o
        /// <c>null</c> si todavía falta el otro jugador.
        /// </returns>
        /// <exception cref="ArgumentException">Alguno de los argumentos es nulo o está en blanco.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.PartidaNoEncontradaException">No existe la partida indicada.</exception>
        Task<(bool reinicioBlancas, bool reinicioNegras, Partida?)> ExecuteAsync(string partidaId, string jugadorId);
    }
}
