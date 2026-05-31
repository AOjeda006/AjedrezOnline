namespace ServidorAjedrez.Domain.Enums
{
    /// <summary>
    /// Fase del ciclo de vida de una <see cref="ServidorAjedrez.Domain.Entities.Partida"/>.
    /// </summary>
    /// <remarks>
    /// Las transiciones válidas son <see cref="Esperando"/> → <see cref="EnCurso"/> →
    /// <see cref="Finalizada"/>. Solo en <see cref="EnCurso"/> se aceptan movimientos
    /// y solicitudes (tablas, rendición, abandono); la revancha vuelve a
    /// <see cref="EnCurso"/> reutilizando la misma instancia.
    /// </remarks>
    public enum EstadoPartida
    {
        /// <summary>Partida creada pero aún no iniciada: sin piezas colocadas ni turnos jugados.</summary>
        Esperando = 0,

        /// <summary>Partida en juego; admite movimientos y el resto de acciones del jugador.</summary>
        EnCurso = 1,

        /// <summary>Partida terminada; el resultado y el <see cref="TipoFinPartida"/> quedan fijados.</summary>
        Finalizada = 2
    }
}
