using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.DTOs
{
    /// <summary>
    /// Forma serializable de una <see cref="Partida"/>, con el estado completo que el cliente
    /// necesita para representarla: tablero, jugadores, turno, estado y banderas de fin de partida.
    /// </summary>
    public class PartidaDTO
    {
        public string? Id { get; set; }
        public string? SalaId { get; set; }
        public TableroDTO? Tablero { get; set; }
        public JugadorDTO? JugadorBlancas { get; set; }
        public JugadorDTO? JugadorNegras { get; set; }
        public Color? TurnoActual { get; set; }
        public int? NumeroTurnos { get; set; }
        /// <summary>Tiempo de juego transcurrido, en segundos enteros (la entidad lo guarda como <see cref="System.TimeSpan"/>).</summary>
        public int? TiempoTranscurrido { get; set; }
        public EstadoPartida? Estado { get; set; }
        public ResultadoPartida? Resultado { get; set; }
        public TipoFinPartida? TipoFin { get; set; }
        public bool? TablasBlancas { get; set; }
        public bool? TablasNegras { get; set; }
        /// <summary>Indica si el bando en turno está en jaque, o <c>null</c> si no se calculó (ver <see cref="FromDomain"/>).</summary>
        public bool? HayJaque { get; set; }

        /// <summary>Indica si el bando en turno está en jaque mate, o <c>null</c> si no se calculó (ver <see cref="FromDomain"/>).</summary>
        public bool? HayJaqueMate { get; set; }

        /// <summary>
        /// Crea el DTO a partir de una partida de dominio.
        /// </summary>
        /// <param name="partida">Partida de dominio a convertir.</param>
        /// <param name="incluirJaque">
        /// Si es <c>true</c> (por defecto), calcula y rellena <see cref="HayJaque"/> y
        /// <see cref="HayJaqueMate"/>; si es <c>false</c>, ambos quedan a <c>null</c> para evitar
        /// ese cálculo cuando no se necesita.
        /// </param>
        /// <returns>El <see cref="PartidaDTO"/> equivalente.</returns>
        public static PartidaDTO FromDomain(Partida partida, bool incluirJaque = true)
        {
            return new PartidaDTO
            {
                Id = partida.Id,
                SalaId = partida.SalaId,
                Tablero = TableroDTO.FromDomain(partida.Tablero),
                JugadorBlancas = JugadorDTO.FromDomain(partida.JugadorBlancas),
                JugadorNegras = JugadorDTO.FromDomain(partida.JugadorNegras),
                TurnoActual = partida.TurnoActual,
                NumeroTurnos = partida.NumeroTurnos,
                TiempoTranscurrido = (int)partida.TiempoTranscurrido.TotalSeconds,
                Estado = partida.Estado,
                Resultado = partida.Resultado,
                TipoFin = partida.TipoFin,
                TablasBlancas = partida.TablasBlancas,
                TablasNegras = partida.TablasNegras,
                HayJaque = incluirJaque ? partida.Tablero.HayJaque(partida.TurnoActual) : null,
                HayJaqueMate = incluirJaque ? partida.Tablero.HayJaqueMate(partida.TurnoActual) : null
            };
        }
    }
}
