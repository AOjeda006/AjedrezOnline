using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Exceptions;
using ServidorAjedrez.Domain.Repositories;

using ServidorAjedrez.Domain.Interfaces;
namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="IUnirseSalaUseCase"/>
    /// <remarks>
    /// Añade el oponente a la sala, crea e inicia la <see cref="Partida"/> y persiste ambos cambios
    /// mediante <see cref="ISalaRepository"/> e <see cref="IPartidaRepository"/>.
    /// </remarks>
    public class UnirseSalaUseCase : IUnirseSalaUseCase
    {
        private readonly ISalaRepository _salaRepository;
        private readonly IPartidaRepository _partidaRepository;
        private readonly ILogger<UnirseSalaUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public UnirseSalaUseCase(ISalaRepository salaRepository, IPartidaRepository partidaRepository, ILogger<UnirseSalaUseCase> logger)
        {
            _salaRepository = salaRepository ?? throw new ArgumentNullException(nameof(salaRepository));
            _partidaRepository = partidaRepository ?? throw new ArgumentNullException(nameof(partidaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Partida> ExecuteAsync(string nombreSala, string nombreJugador, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(nombreSala))
                throw new ArgumentException("El nombre de la sala no puede estar vacío.", nameof(nombreSala));
            if (string.IsNullOrWhiteSpace(nombreJugador))
                throw new ArgumentException("El nombre del jugador no puede estar vacío.", nameof(nombreJugador));
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Jugador {nombreJugador} intenta unirse a sala: {nombreSala}");

                var sala = await _salaRepository.ObtenerPorNombreAsync(nombreSala);
                if (sala == null)
                    throw new SalaNoEncontradaException(nombreSala);

                if (sala.EstaCompleta())
                    throw new SalaCompletaException(nombreSala);

                var oponente = Jugador.Create(nombreJugador, connectionId);
                sala.AgregarOponente(oponente);

                await _salaRepository.ActualizarAsync(sala);

                // Crear partida
                var partida = Partida.Create(sala.Id, sala.Creador, oponente);
                partida.IniciarPartida();

                var partidaCreada = await _partidaRepository.CrearAsync(partida);
                sala.IniciarPartida(partidaCreada.Id);

                await _salaRepository.ActualizarAsync(sala);

                _logger.LogInformation($"Partida iniciada con ID: {partidaCreada.Id}");

                return partidaCreada;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al unirse a sala: {ex.Message}");
                throw;
            }
        }
    }
}
