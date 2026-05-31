namespace ServidorAjedrez.Domain.Enums
{
    /// <summary>
    /// Bando de ajedrez. Identifica tanto a las piezas como al jugador que las
    /// controla y de quién es el turno en cada momento.
    /// </summary>
    /// <remarks>
    /// Se serializa como texto ("Blanca" / "Negra") hacia el cliente gracias al
    /// <c>JsonStringEnumConverter</c> configurado en <c>Program.cs</c>; el valor
    /// numérico subyacente no debe asumirse en el frontend. Las blancas mueven primero.
    /// </remarks>
    public enum Color
    {
        /// <summary>Bando blanco; realiza siempre el primer movimiento de la partida.</summary>
        Blanca = 0,

        /// <summary>Bando negro.</summary>
        Negra = 1
    }
}
