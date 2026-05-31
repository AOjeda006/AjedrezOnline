namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Caso de uso que registra la oferta de tablas de un jugador.
    /// </summary>
    public interface ISolicitarTablasUseCase
    {
        /// <summary>
        /// Anota la oferta de tablas del jugador; si ambos las han ofrecido, la partida acaba en empate.
        /// </summary>
        /// <param name="partidaId">Id de la partida; no puede ser nulo ni estar en blanco.</param>
        /// <param name="jugadorId">Identificador de conexión SignalR del jugador que ofrece tablas; se resuelve al jugador de dominio.</param>
        /// <returns>
        /// Una tupla <c>(tablasBlancas, tablasNegras, aceptadas)</c> con el estado de la oferta de
        /// cada bando y si las tablas han quedado aceptadas (ambos de acuerdo).
        /// </returns>
        /// <exception cref="ArgumentException">Alguno de los argumentos es nulo o está en blanco.</exception>
        /// <exception cref="ServidorAjedrez.Domain.Exceptions.PartidaNoEncontradaException">No existe la partida indicada.</exception>
        Task<(bool tablasBlancas, bool tablasNegras, bool aceptadas)> ExecuteAsync(string partidaId, string jugadorId);
    }
}
