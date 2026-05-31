namespace ServidorAjedrez.Domain.Exceptions
{
    /// <summary>
    /// Se lanza cuando no existe ninguna partida con el identificador indicado.
    /// </summary>
    /// <remarks>
    /// La originan los casos de uso al resolver una partida por su id (por ejemplo tras una
    /// desconexión, una sala ya cerrada o un id inválido enviado por el cliente). El hub la
    /// captura para informar al jugador sin tratarla como error inesperado.
    /// </remarks>
    public class PartidaNoEncontradaException : DomainException
    {
        /// <summary>Crea la excepción componiendo un mensaje con el identificador buscado.</summary>
        /// <param name="partidaId">Identificador de la partida que no se encontró.</param>
        public PartidaNoEncontradaException(string partidaId)
            : base($"La partida con ID '{partidaId}' no fue encontrada.") { }
    }
}
