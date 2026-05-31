namespace ServidorAjedrez.Domain.Exceptions
{
    /// <summary>
    /// Representa una acción intentada por un jugador cuando no es su turno.
    /// </summary>
    /// <remarks>
    /// Tipo disponible para señalar específicamente el turno incorrecto. El dominio vigente
    /// rechaza estas situaciones con <see cref="InvalidOperationException"/>, por lo que esta
    /// excepción todavía no se lanza en el flujo actual.
    /// </remarks>
    public class TurnoInvalidoException : DomainException
    {
        /// <summary>Crea la excepción componiendo un mensaje con el jugador afectado.</summary>
        /// <param name="jugadorId">Identificador del jugador que intentó actuar fuera de su turno.</param>
        public TurnoInvalidoException(string jugadorId)
            : base($"No es el turno del jugador {jugadorId}.") { }
    }
}
