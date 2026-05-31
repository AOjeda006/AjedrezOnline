using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Enums;
using ServidorAjedrez.Domain.Exceptions;
using ServidorAjedrez.Domain.Repositories;

using ServidorAjedrez.Domain.Interfaces;
namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="IRendirseUseCase"/>
    public class RendirseUseCase : IRendirseUseCase
    {
        private readonly IPartidaRepository _partidaRepository;
        private readonly ISalaRepository _salaRepository;
        private readonly ILogger<RendirseUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public RendirseUseCase(IPartidaRepository partidaRepository, ISalaRepository salaRepository, ILogger<RendirseUseCase> logger)
        {
            _partidaRepository = partidaRepository ?? throw new ArgumentNullException(nameof(partidaRepository));
            _salaRepository = salaRepository ?? throw new ArgumentNullException(nameof(salaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        /// <remarks>Además de finalizar la partida por rendición, marca como finalizada la sala asociada.</remarks>
        public async Task<(ResultadoPartida, string)> ExecuteAsync(string partidaId, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(partidaId))
                throw new ArgumentException("El ID de la partida no puede estar vacío.", nameof(partidaId));
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId del jugador no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Jugador con connectionId {connectionId} se rinde en partida {partidaId}");

                var partida = await _partidaRepository.ObtenerPorIdAsync(partidaId);
                if (partida == null)
                    throw new PartidaNoEncontradaException(partidaId);

                // FIX: Convert ConnectionId to Jugador.Id
                var jugadorId = partida.ObtenerJugadorIdPorConnectionId(connectionId);

                partida.Rendirse(jugadorId);
                await _partidaRepository.ActualizarAsync(partida);

                // Finalizar sala
                var sala = await _salaRepository.ObtenerPorIdAsync(partida.SalaId);
                if (sala != null)
                {
                    sala.FinalizarPartida();
                    await _salaRepository.ActualizarAsync(sala);
                }

                _logger.LogInformation($"Partida finalizada por rendición");

                return (partida.Resultado ?? ResultadoPartida.Empate, jugadorId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al rendirse: {ex.Message}");
                throw;
            }
        }
    }
}
