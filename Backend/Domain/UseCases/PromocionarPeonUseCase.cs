using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Enums;
using ServidorAjedrez.Domain.Exceptions;
using ServidorAjedrez.Domain.Repositories;

using ServidorAjedrez.Domain.Interfaces;
namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="IPromocionarPeonUseCase"/>
    public class PromocionarPeonUseCase : IPromocionarPeonUseCase
    {
        private readonly IPartidaRepository _partidaRepository;
        private readonly ILogger<PromocionarPeonUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public PromocionarPeonUseCase(IPartidaRepository partidaRepository, ILogger<PromocionarPeonUseCase> logger)
        {
            _partidaRepository = partidaRepository ?? throw new ArgumentNullException(nameof(partidaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        /// <remarks>
        /// La coronación puede provocar jaque mate; en ese caso <c>Partida.PromocionarPeon</c> ya
        /// finaliza la partida y aquí solo se traslada el resultado a la tupla devuelta.
        /// </remarks>
        public async Task<(Tablero, bool, ResultadoPartida?, string?)> ExecuteAsync(string partidaId, TipoPieza tipo, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(partidaId))
                throw new ArgumentException("El ID de la partida no puede estar vacío.", nameof(partidaId));
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId del jugador no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Promocionando peón a {tipo} en partida {partidaId}");

                var partida = await _partidaRepository.ObtenerPorIdAsync(partidaId);
                if (partida == null)
                    throw new PartidaNoEncontradaException(partidaId);

                // FIX: Convert ConnectionId to Jugador.Id
                var jugadorId = partida.ObtenerJugadorIdPorConnectionId(connectionId);

                partida.PromocionarPeon(tipo, jugadorId);
                await _partidaRepository.ActualizarAsync(partida);

                // La coronación puede dar jaque mate; PromocionarPeon ya finaliza la partida en ese caso.
                bool hayJaqueMate = partida.Estado == EstadoPartida.Finalizada
                    && partida.TipoFin == TipoFinPartida.JaqueMate;

                ResultadoPartida? resultado = null;
                string? ganadorId = null;
                if (hayJaqueMate)
                {
                    resultado = partida.Resultado;
                    ganadorId = partida.TurnoActual == Color.Blanca
                        ? partida.JugadorNegras.Id
                        : partida.JugadorBlancas.Id;
                }

                _logger.LogInformation($"Peón promocionado exitosamente. Jaque mate: {hayJaqueMate}");

                return (partida.Tablero, hayJaqueMate, resultado, ganadorId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al promocionar peón: {ex.Message}");
                throw;
            }
        }
    }
}
