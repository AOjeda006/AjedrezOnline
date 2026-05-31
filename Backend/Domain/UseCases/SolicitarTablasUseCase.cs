using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Exceptions;
using ServidorAjedrez.Domain.Repositories;

using ServidorAjedrez.Domain.Interfaces;
namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="ISolicitarTablasUseCase"/>
    public class SolicitarTablasUseCase : ISolicitarTablasUseCase
    {
        private readonly IPartidaRepository _partidaRepository;
        private readonly ILogger<SolicitarTablasUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public SolicitarTablasUseCase(IPartidaRepository partidaRepository, ILogger<SolicitarTablasUseCase> logger)
        {
            _partidaRepository = partidaRepository ?? throw new ArgumentNullException(nameof(partidaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<(bool tablasBlancas, bool tablasNegras, bool aceptadas)> ExecuteAsync(string partidaId, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(partidaId))
                throw new ArgumentException("El ID de la partida no puede estar vacío.", nameof(partidaId));
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId del jugador no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Solicitando tablas en partida {partidaId}");

                var partida = await _partidaRepository.ObtenerPorIdAsync(partidaId);
                if (partida == null)
                    throw new PartidaNoEncontradaException(partidaId);

                // FIX: Convert ConnectionId to Jugador.Id
                var jugadorId = partida.ObtenerJugadorIdPorConnectionId(connectionId);

                partida.SolicitarTablas(jugadorId);
                await _partidaRepository.ActualizarAsync(partida);

                bool aceptadas = partida.TablasBlancas && partida.TablasNegras;

                _logger.LogInformation($"Solicitud de tablas registrada. Aceptadas: {aceptadas}");

                return (partida.TablasBlancas, partida.TablasNegras, aceptadas);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al solicitar tablas: {ex.Message}");
                throw;
            }
        }
    }
}
