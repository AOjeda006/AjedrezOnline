using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Interfaces;
using ServidorAjedrez.Domain.Repositories;

namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="ICrearSalaUseCase"/>
    /// <remarks>Persiste la sala mediante <see cref="ISalaRepository"/>; registra la operación y propaga al llamador cualquier error.</remarks>
    public class CrearSalaUseCase : ICrearSalaUseCase
    {
        private readonly ISalaRepository _salaRepository;
        private readonly ILogger<CrearSalaUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public CrearSalaUseCase(ISalaRepository salaRepository, ILogger<CrearSalaUseCase> logger)
        {
            _salaRepository = salaRepository ?? throw new ArgumentNullException(nameof(salaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Sala> ExecuteAsync(string nombreSala, string nombreJugador, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(nombreSala))
                throw new ArgumentException("El nombre de la sala no puede estar vacío.", nameof(nombreSala));
            if (string.IsNullOrWhiteSpace(nombreJugador))
                throw new ArgumentException("El nombre del jugador no puede estar vacío.", nameof(nombreJugador));
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Creando sala con nombre: {nombreSala}, creador: {nombreJugador}");

                // FIX: Use nombreJugador for the Jugador, not nombreSala
                var creador = Jugador.Create(nombreJugador, connectionId);
                var sala = Sala.Create(nombreSala, creador);

                var salaCreada = await _salaRepository.CrearAsync(sala);
                _logger.LogInformation($"Sala creada exitosamente con ID: {salaCreada.Id}");

                return salaCreada;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al crear sala: {ex.Message}");
                throw;
            }
        }
    }
}
