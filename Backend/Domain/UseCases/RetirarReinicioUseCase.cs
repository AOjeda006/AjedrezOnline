using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Exceptions;
using ServidorAjedrez.Domain.Repositories;

using ServidorAjedrez.Domain.Interfaces;
namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="IRetirarReinicioUseCase"/>
    public class RetirarReinicioUseCase : IRetirarReinicioUseCase
    {
        private readonly IPartidaRepository _partidaRepository;
        private readonly ILogger<RetirarReinicioUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public RetirarReinicioUseCase(IPartidaRepository partidaRepository, ILogger<RetirarReinicioUseCase> logger)
        {
            _partidaRepository = partidaRepository ?? throw new ArgumentNullException(nameof(partidaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<(bool reinicioBlancas, bool reinicioNegras)> ExecuteAsync(string partidaId, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(partidaId))
                throw new ArgumentException("El ID de la partida no puede estar vacío.", nameof(partidaId));
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId del jugador no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Retirando solicitud de reinicio en partida {partidaId}");

                var partida = await _partidaRepository.ObtenerPorIdAsync(partidaId);
                if (partida == null)
                    throw new PartidaNoEncontradaException(partidaId);

                // FIX: Convert ConnectionId to Jugador.Id
                var jugadorId = partida.ObtenerJugadorIdPorConnectionId(connectionId);

                partida.RetirarReinicio(jugadorId);
                await _partidaRepository.ActualizarAsync(partida);

                _logger.LogInformation($"Solicitud de reinicio retirada");

                return (partida.ReinicioBlancas, partida.ReinicioNegras);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al retirar reinicio: {ex.Message}");
                throw;
            }
        }
    }
}
