using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.DTOs;
using ServidorAjedrez.Domain.UseCases.Mappers;
using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Exceptions;
using ServidorAjedrez.Domain.Interfaces;
using ServidorAjedrez.Domain.Repositories;

namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="IRealizarMovimientoUseCase"/>
    /// <remarks>
    /// Resuelve la conexión al jugador de dominio, convierte el DTO con
    /// <see cref="Mappers.MovimientoMapper"/> y persiste la partida con el movimiento pendiente.
    /// </remarks>
    public class RealizarMovimientoUseCase : IRealizarMovimientoUseCase
    {
        private readonly IPartidaRepository _partidaRepository;
        private readonly ILogger<RealizarMovimientoUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public RealizarMovimientoUseCase(IPartidaRepository partidaRepository, ILogger<RealizarMovimientoUseCase> logger)
        {
            _partidaRepository = partidaRepository ?? throw new ArgumentNullException(nameof(partidaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<(Movimiento, Tablero)> ExecuteAsync(string partidaId, MovimientoDTO movimientoDTO, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(partidaId))
                throw new ArgumentException("El ID de la partida no puede estar vacío.", nameof(partidaId));
            if (movimientoDTO == null)
                throw new ArgumentNullException(nameof(movimientoDTO));
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId del jugador no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Realizando movimiento en partida {partidaId}");

                var partida = await _partidaRepository.ObtenerPorIdAsync(partidaId);
                if (partida == null)
                    throw new PartidaNoEncontradaException(partidaId);

                // FIX: Convert ConnectionId to Jugador.Id
                var jugadorId = partida.ObtenerJugadorIdPorConnectionId(connectionId);

                var movimiento = MovimientoMapper.FromDTO(movimientoDTO);
                partida.RealizarMovimiento(movimiento, jugadorId);

                await _partidaRepository.ActualizarAsync(partida);

                _logger.LogInformation($"Movimiento realizado exitosamente");

                return (movimiento, partida.Tablero);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al realizar movimiento: {ex.Message}");
                throw;
            }
        }
    }
}
