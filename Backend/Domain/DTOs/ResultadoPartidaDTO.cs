using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.DTOs
{
    /// <summary>
    /// Resultado de una partida en forma serializable: el desenlace, su motivo y el bando ganador.
    /// </summary>
    public class ResultadoPartidaDTO
    {
        /// <summary>Desenlace de la partida (victoria de un bando o empate).</summary>
        public ResultadoPartida Resultado { get; set; }

        /// <summary>Motivo del fin de la partida, o <c>null</c> si no se especifica.</summary>
        public TipoFinPartida? TipoFin { get; set; }

        /// <summary>Color del bando ganador, o <c>null</c> en caso de empate o si no aplica.</summary>
        public Color? Ganador { get; set; }

        /// <summary>Crea un DTO vacío (para la deserialización).</summary>
        public ResultadoPartidaDTO() { }

        /// <summary>Crea el DTO con todos sus datos.</summary>
        /// <param name="resultado">Desenlace de la partida.</param>
        /// <param name="tipoFin">Motivo del fin de la partida.</param>
        /// <param name="ganador">Color del bando ganador, o <c>null</c> si no aplica.</param>
        public ResultadoPartidaDTO(ResultadoPartida resultado, TipoFinPartida? tipoFin, Color? ganador)
        {
            Resultado = resultado;
            TipoFin = tipoFin;
            Ganador = ganador;
        }
    }
}