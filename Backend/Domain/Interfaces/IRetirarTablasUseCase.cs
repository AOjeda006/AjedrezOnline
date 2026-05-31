namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que retira la oferta de tablas de un jugador.
    /// </summary>
    public interface IRetirarTablasUseCase
    {
        /// <summary>Retira la oferta de tablas del jugador indicado.</summary>
        /// <param name="partidaId">Id de la partida; no puede ser nulo ni estar en blanco.</param>
        /// <param name="jugadorId">Identificador de conexión SignalR del jugador; se resuelve al jugador de dominio.</param>
        /// <returns>Una tupla <c>(tablasBlancas, tablasNegras)</c> con el estado de la oferta de cada bando tras la retirada.</returns>
        /// <exception cref="ArgumentException">Alguno de los argumentos es nulo o está en blanco.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.PartidaNoEncontradaException">No existe la partida indicada.</exception>
        Task<(bool tablasBlancas, bool tablasNegras)> ExecuteAsync(string partidaId, string jugadorId);
    }
}
