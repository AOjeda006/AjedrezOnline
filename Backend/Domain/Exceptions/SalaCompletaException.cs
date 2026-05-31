namespace ServidorAjedrez.Domain.Exceptions
{
    /// <summary>
    /// Se lanza al intentar unirse a una sala que ya tiene a sus dos jugadores.
    /// </summary>
    public class SalaCompletaException : DomainException
    {
        /// <summary>Crea la excepción componiendo un mensaje con el nombre de la sala.</summary>
        /// <param name="nombreSala">Nombre de la sala que ya está completa.</param>
        public SalaCompletaException(string nombreSala)
            : base($"La sala '{nombreSala}' está completa.") { }
    }
}
