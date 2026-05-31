using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Exceptions;
using ServidorAjedrez.Domain.Repositories;

using ServidorAjedrez.Domain.Interfaces;
namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="ISolicitarReinicioUseCase"/>
    public class SolicitarReinicioUseCase : ISolicitarReinicioUseCase
    {
        private readonly IPartidaRepository _partidaRepository;
        private readonly ISalaRepository _salaRepository;
        private readonly ILogger<SolicitarReinicioUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public SolicitarReinicioUseCase(IPartidaRepository partidaRepository, ISalaRepository salaRepository, ILogger<SolicitarReinicioUseCase> logger)
        {
            _partidaRepository = partidaRepository ?? throw new ArgumentNullException(nameof(partidaRepository));
            _salaRepository = salaRepository ?? throw new ArgumentNullException(nameof(salaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Detecta la revancha de forma indirecta: como <c>Partida.AceptarReinicio</c> (invocada
        /// dentro de <c>SolicitarReinicio</c> cuando ambos aceptan) pone las dos banderas a
        /// <c>false</c> al reiniciar, este caso de uso asume que la revancha se ha producido cuando,
        /// tras registrar la solicitud, ninguna de las dos banderas queda activa.
        /// </remarks>
        public async Task<(bool reinicioBlancas, bool reinicioNegras, Partida?)> ExecuteAsync(string partidaId, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(partidaId))
                throw new ArgumentException("El ID de la partida no puede estar vacío.", nameof(partidaId));
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId del jugador no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Solicitando reinicio en partida {partidaId}");

                var partida = await _partidaRepository.ObtenerPorIdAsync(partidaId);
                if (partida == null)
                    throw new PartidaNoEncontradaException(partidaId);

                // FIX: Convert ConnectionId to Jugador.Id
                var jugadorId = partida.ObtenerJugadorIdPorConnectionId(connectionId);

                partida.SolicitarReinicio(jugadorId);
                await _partidaRepository.ActualizarAsync(partida);

                // FIX: AceptarReinicio() (called inside SolicitarReinicio when both agree)
                // resets both flags to false BEFORE we get here. So checking both=true never works.
                // Instead: if both flags are false after we just set ours to true,
                // it means AceptarReinicio was triggered (reset them after restarting the game).
                bool reiniciada = !partida.ReinicioBlancas && !partida.ReinicioNegras;

                _logger.LogInformation($"Solicitud de reinicio registrada. Reiniciada: {reiniciada}");

                return (partida.ReinicioBlancas, partida.ReinicioNegras, reiniciada ? partida : null);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al solicitar reinicio: {ex.Message}");
                throw;
            }
        }
    }
}
