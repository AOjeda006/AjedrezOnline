namespace ServidorAjedrez.Domain.Exceptions
{
    /// <summary>
    /// Representa el intento de aplicar un movimiento que infringe las reglas del ajedrez.
    /// </summary>
    /// <remarks>
    /// Tipo disponible para señalar movimientos ilegales de forma específica. La validación
    /// vigente del dominio (<c>Partida.RealizarMovimiento</c> y <c>Tablero.EsMovimientoValido</c>)
    /// rechaza estos casos con <see cref="InvalidOperationException"/>, de modo que esta
    /// excepción aún no se lanza en el flujo actual.
    /// </remarks>
    public class MovimientoInvalidoException : DomainException
    {
        /// <summary>Crea la excepción con el motivo concreto de la invalidez del movimiento.</summary>
        /// <param name="mensaje">Descripción de por qué el movimiento no es legal.</param>
        public MovimientoInvalidoException(string mensaje)
            : base(mensaje) { }
    }
}
