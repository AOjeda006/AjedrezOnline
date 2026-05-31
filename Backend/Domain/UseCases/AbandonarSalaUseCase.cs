using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Enums;
using ServidorAjedrez.Domain.Repositories;

using ServidorAjedrez.Domain.Interfaces;
namespace ServidorAjedrez.Domain.UseCases
{
    /// <inheritdoc cref="IAbandonarSalaUseCase"/>
    /// <remarks>
    /// Localiza la sala buscando entre todas la que contiene la conexión (como creador u oponente).
    /// </remarks>
    public class AbandonarSalaUseCase : IAbandonarSalaUseCase
    {
        private readonly ISalaRepository _salaRepository;
        private readonly IPartidaRepository _partidaRepository;
        private readonly ILogger<AbandonarSalaUseCase> _logger;

        /// <summary>Crea el caso de uso con sus dependencias.</summary>
        /// <exception cref="ArgumentNullException">Alguna de las dependencias es nula.</exception>
        public AbandonarSalaUseCase(ISalaRepository salaRepository, IPartidaRepository partidaRepository, ILogger<AbandonarSalaUseCase> logger)
        {
            _salaRepository = salaRepository ?? throw new ArgumentNullException(nameof(salaRepository));
            _partidaRepository = partidaRepository ?? throw new ArgumentNullException(nameof(partidaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Solo finaliza la partida por abandono si esta seguía <see cref="EstadoPartida.EnCurso"/>,
        /// para no sobrescribir un resultado ya fijado (jaque mate, tablas o rendición). Si la
        /// conexión no pertenece a ninguna sala, devuelve la tupla con todos los elementos vacíos o nulos.
        /// </remarks>
        public async Task<(string salaId, string partidaId, ResultadoPartida?, string?)> ExecuteAsync(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId no puede estar vacío.", nameof(connectionId));

            try
            {
                _logger.LogInformation($"Jugador con connectionId {connectionId} abandona la sala");

                var salas = await _salaRepository.ObtenerTodasAsync();
                var sala = salas.FirstOrDefault(s =>
                    s.Creador.ConnectionId == connectionId ||
                    s.Oponente?.ConnectionId == connectionId);

                if (sala == null)
                    return ("", "", null, null);

                string salaId = sala.Id;
                string partidaId = sala.PartidaActualId ?? "";
                ResultadoPartida? resultado = null;
                string? ganadorId = null;

                // Marcar partida como finalizada por abandono si existe
                if (!string.IsNullOrEmpty(partidaId))
                {
                    var partida = await _partidaRepository.ObtenerPorIdAsync(partidaId);
                    // Solo finalizar por abandono si la partida seguía en curso (no pisar un
                    // resultado por jaque mate / tablas / rendición ya existente)
                    if (partida != null && partida.Estado == EstadoPartida.EnCurso)
                    {
                        ganadorId = sala.Creador.ConnectionId == connectionId ? sala.Oponente?.Id : sala.Creador.Id;
                        if (!string.IsNullOrEmpty(ganadorId))
                        {
                            partida.Finalizar(TipoFinPartida.Abandono, ganadorId);
                            await _partidaRepository.ActualizarAsync(partida);
                            resultado = partida.Resultado;
                        }
                    }
                }

                // Eliminar sala
                await _salaRepository.EliminarAsync(salaId);

                _logger.LogInformation($"Jugador ha abandonado la sala {salaId}");

                return (salaId, partidaId, resultado, ganadorId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al abandonar sala: {ex.Message}");
                throw;
            }
        }
    }
}
