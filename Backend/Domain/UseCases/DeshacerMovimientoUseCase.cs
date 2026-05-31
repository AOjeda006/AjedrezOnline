using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Exceptions;
using ServidorAjedrez.Domain.Repositories;

using ServidorAjedrez.Domain.Interfaces;
namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="IDeshacerMovimientoUseCase"/>
    public class DeshacerMovimientoUseCase : IDeshacerMovimientoUseCase
    {
        private readonly IPartidaRepository _partidaRepository;
        private readonly ILogger<DeshacerMovimientoUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public DeshacerMovimientoUseCase(IPartidaRepository partidaRepository, ILogger<DeshacerMovimientoUseCase> logger)
        {
            _partidaRepository = partidaRepository ?? throw new ArgumentNullException(nameof(partidaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Tablero> ExecuteAsync(string partidaId, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(partidaId))
                throw new ArgumentException("El ID de la partida no puede estar vacío.", nameof(partidaId));
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId del jugador no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Deshaciendo movimiento en partida {partidaId}");

                var partida = await _partidaRepository.ObtenerPorIdAsync(partidaId);
                if (partida == null)
                    throw new PartidaNoEncontradaException(partidaId);

                // FIX: Convert ConnectionId to Jugador.Id
                var jugadorId = partida.ObtenerJugadorIdPorConnectionId(connectionId);

                partida.DeshacerMovimiento(jugadorId);
                await _partidaRepository.ActualizarAsync(partida);

                _logger.LogInformation($"Movimiento deshecho exitosamente");

                return partida.Tablero;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al deshacer movimiento: {ex.Message}");
                throw;
            }
        }
    }
}
