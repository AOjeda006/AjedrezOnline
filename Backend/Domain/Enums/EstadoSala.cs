namespace ServidorAjedrez.Domain.Enums
{
    /// <summary>
    /// Fase del ciclo de vida de una <see cref="ServidorAjedrez.Domain.Entities.Sala"/>
    /// (lobby donde se emparejan dos jugadores).
    /// </summary>
    /// <remarks>
    /// La sala nace en <see cref="Esperando"/> a la espera de oponente; pasa a
    /// <see cref="EnCurso"/> al iniciarse la partida y a <see cref="Finalizada"/>
    /// cuando esta termina. Es independiente del <see cref="EstadoPartida"/>, aunque
    /// suelen evolucionar en paralelo.
    /// </remarks>
    public enum EstadoSala
    {
        /// <summary>Sala creada con un único jugador, esperando a que se una un oponente.</summary>
        Esperando = 0,

        /// <summary>Sala con ambos jugadores y una partida en juego.</summary>
        EnCurso = 1,

        /// <summary>Sala cuya partida ha terminado.</summary>
        Finalizada = 2
    }
}
