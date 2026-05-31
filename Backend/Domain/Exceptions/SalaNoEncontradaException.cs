namespace ServidorAjedrez.Domain.Exceptions
{
    /// <summary>
    /// Se lanza al intentar unirse a una sala cuyo nombre no corresponde a ninguna sala
    /// abierta (inexistente o que ya tiene oponente y, por tanto, no admite búsqueda por nombre).
    /// </summary>
    public class SalaNoEncontradaException : DomainException
    {
        /// <summary>Crea la excepción componiendo un mensaje con el nombre buscado.</summary>
        /// <param name="nombreSala">Nombre de la sala que no se encontró.</param>
        public SalaNoEncontradaException(string nombreSala)
            : base($"La sala '{nombreSala}' no fue encontrada.") { }
    }
}
