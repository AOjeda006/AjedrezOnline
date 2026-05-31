namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que retira la solicitud de revancha de un jugador.
    /// </summary>
    public interface IRetirarReinicioUseCase
    {
        /// <summary>Retira la solicitud de revancha del jugador indicado.</summary>
        /// <param name="partidaId">Id de la partida; no puede ser nulo ni estar en blanco.</param>
        /// <param name="jugadorId">Identificador de conexión SignalR del jugador; se resuelve al jugador de dominio.</param>
        /// <returns>Una tupla <c>(reinicioBlancas, reinicioNegras)</c> con el estado de la solicitud de cada bando tras la retirada.</returns>
        /// <exception cref="ArgumentException">Alguno de los argumentos es nulo o está en blanco.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.PartidaNoEncontradaException">No existe la partida indicada.</exception>
        Task<(bool reinicioBlancas, bool reinicioNegras)> ExecuteAsync(string partidaId, string jugadorId);
    }
}
