using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Exceptions;
using ServidorAjedrez.Domain.Repositories;

using ServidorAjedrez.Domain.Interfaces;
namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="IConfirmarMovimientoUseCase"/>
    public class ConfirmarMovimientoUseCase : IConfirmarMovimientoUseCase
    {
        private readonly IPartidaRepository _partidaRepository;
        private readonly ILogger<ConfirmarMovimientoUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public ConfirmarMovimientoUseCase(IPartidaRepository partidaRepository, ILogger<ConfirmarMovimientoUseCase> logger)
        {
            _partidaRepository = partidaRepository ?? throw new ArgumentNullException(nameof(partidaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Toma el estado de la partida como fuente de verdad para el jaque mate: en una promoción
        /// el mate se reevalúa tras coronar, por lo que aquí no se anticipa. El ganador se deduce
        /// como el bando contrario al que tiene el turno (el que está en mate).
        /// </remarks>
        public async Task<(ServidorAjedrez.Domain.Enums.Color, int, bool, bool, ServidorAjedrez.Domain.Enums.ResultadoPartida?, string?)> ExecuteAsync(string partidaId, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(partidaId))
                throw new ArgumentException("El ID de la partida no puede estar vacío.", nameof(partidaId));
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId del jugador no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Confirmando movimiento en partida {partidaId}");

                var partida = await _partidaRepository.ObtenerPorIdAsync(partidaId);
                if (partida == null)
                    throw new PartidaNoEncontradaException(partidaId);

                // FIX: Convert ConnectionId to Jugador.Id
                var jugadorId = partida.ObtenerJugadorIdPorConnectionId(connectionId);

                // Check if the pending movement is a promotion before confirming
                bool esPromocion = partida.MovimientoPendiente?.EsPromocion ?? false;

                partida.ConfirmarMovimiento(jugadorId);
                await _partidaRepository.ActualizarAsync(partida);

                // La partida solo se finaliza por jaque mate dentro de ConfirmarMovimiento cuando
                // NO es promoción (en ese caso el mate se reevalúa tras coronar). Usamos el estado
                // de la partida como fuente de verdad para no anticipar el mate en una promoción.
                bool hayJaqueMate = partida.Estado == ServidorAjedrez.Domain.Enums.EstadoPartida.Finalizada
                    && partida.TipoFin == ServidorAjedrez.Domain.Enums.TipoFinPartida.JaqueMate;

                ServidorAjedrez.Domain.Enums.ResultadoPartida? resultado = null;
                string? ganadorId = null;
                if (hayJaqueMate)
                {
                    resultado = partida.Resultado;
                    // El jugador en jaque mate es el del turno actual; el ganador es el contrario.
                    ganadorId = partida.TurnoActual == ServidorAjedrez.Domain.Enums.Color.Blanca
                        ? partida.JugadorNegras.Id
                        : partida.JugadorBlancas.Id;
                }

                _logger.LogInformation($"Movimiento confirmado. Turno actual: {partida.TurnoActual}, Jaque mate: {hayJaqueMate}, Promoción: {esPromocion}");

                return (partida.TurnoActual, partida.NumeroTurnos, hayJaqueMate, esPromocion, resultado, ganadorId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al confirmar movimiento: {ex.Message}");
                throw;
            }
        }
    }
}
