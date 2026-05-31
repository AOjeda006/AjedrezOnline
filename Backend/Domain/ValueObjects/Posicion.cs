namespace ServidorAjedrez.Domain.ValueObjects
{
    /// <summary>
    /// Coordenada inmutable de una casilla del tablero, como par fila/columna en base cero.
    /// Es un objeto de valor: dos posiciones con la misma fila y columna son iguales.
    /// </summary>
    /// <remarks>
    /// <para>
    /// El sistema de coordenadas está alineado con el frontend: la fila 0 es el borde de
    /// las negras y la fila 7 el de las blancas (véase
    /// <see cref="ServidorAjedrez.Domain.Entities.Tablero.InicializarPiezas"/>).
    /// </para>
    /// <para>
    /// El constructor primario <strong>no</strong> valida el rango, de forma deliberada: el
    /// motor de movimientos genera posiciones fuera del tablero como paso intermedio y luego
    /// las descarta con <see cref="EsValida"/>. Usa <see cref="Create"/> cuando la posición
    /// provenga de datos externos y deba garantizarse que cae dentro del tablero.
    /// </para>
    /// </remarks>
    /// <param name="Fila">Índice de fila en base cero; se considera dentro del tablero en el rango 0–7.</param>
    /// <param name="Columna">Índice de columna en base cero; se considera dentro del tablero en el rango 0–7.</param>
    public record Posicion(int Fila, int Columna)
    {
        /// <summary>
        /// Indica si la posición cae dentro de los límites del tablero de 8×8.
        /// </summary>
        /// <returns>
        /// <c>true</c> si <see cref="Fila"/> y <see cref="Columna"/> están ambas en el
        /// rango 0–7; en caso contrario, <c>false</c>.
        /// </returns>
        public bool EsValida()
        {
            return Fila >= 0 && Fila < 8 && Columna >= 0 && Columna < 8;
        }

        /// <summary>
        /// Crea una <see cref="Posicion"/> validando que esté dentro del tablero.
        /// </summary>
        /// <param name="fila">Índice de fila en base cero (0–7).</param>
        /// <param name="columna">Índice de columna en base cero (0–7).</param>
        /// <returns>La posición validada.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="fila"/> o <paramref name="columna"/> quedan fuera del rango 0–7.
        /// </exception>
        public static Posicion Create(int fila, int columna)
        {
            var posicion = new Posicion(fila, columna);
            if (!posicion.EsValida())
                throw new ArgumentException($"Posición inválida: Fila {fila}, Columna {columna}");
            return posicion;
        }
    }
}
