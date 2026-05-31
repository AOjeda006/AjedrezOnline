namespace ServidorAjedrez.Domain.Enums
{
    /// <summary>
    /// Motivo por el que ha terminado una partida. Complementa a
    /// <see cref="ResultadoPartida"/>: este indica <em>por qué</em> acabó y aquel
    /// <em>quién</em> ganó.
    /// </summary>
    /// <remarks>
    /// Acompaña al evento <c>PartidaFinalizada</c> que el hub emite a ambos jugadores,
    /// de modo que el cliente pueda mostrar el mensaje adecuado a cada desenlace.
    /// </remarks>
    public enum TipoFinPartida
    {
        /// <summary>El rey en turno está en jaque y no dispone de ningún movimiento legal.</summary>
        JaqueMate = 0,

        /// <summary>Tablas acordadas: ambos jugadores aceptaron el empate.</summary>
        Tablas = 1,

        /// <summary>Un jugador se rindió de forma explícita.</summary>
        Rendicion = 2,

        /// <summary>Un jugador dejó la sala o perdió la conexión durante la partida en curso.</summary>
        Abandono = 3
    }
}
