namespace ServidorAjedrez.Domain.Enums
{
    /// <summary>
    /// Desenlace de una partida desde el punto de vista del marcador (quién gana o si
    /// hay empate), con independencia del motivo concreto recogido en
    /// <see cref="TipoFinPartida"/>.
    /// </summary>
    /// <remarks>
    /// Una rendición o un abandono producen la victoria del rival; las tablas (acordadas
    /// o por petición mutua) producen <see cref="Empate"/>. El jugador ganador concreto se
    /// transmite aparte como identificador, ya que el color no se conoce solo con este enum.
    /// </remarks>
    public enum ResultadoPartida
    {
        /// <summary>Ganan las blancas.</summary>
        VictoriaBlancas = 0,

        /// <summary>Ganan las negras.</summary>
        VictoriaNegras = 1,

        /// <summary>Tablas: ningún bando gana.</summary>
        Empate = 2
    }
}
