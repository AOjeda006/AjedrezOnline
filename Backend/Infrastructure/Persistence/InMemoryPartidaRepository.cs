using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Repositories;

namespace ServidorAjedrez.Infrastructure.Persistence
{
    /// <inheritdoc cref="IPartidaRepository"/>
    /// <remarks>
    /// Almacenamiento en memoria, sin persistencia: los datos se pierden al reiniciar el servidor.
    /// Está pensado para registrarse como <c>Singleton</c> y serializa todos los accesos con un
    /// <see cref="System.Threading.SemaphoreSlim"/> para ser seguro ante operaciones concurrentes.
    /// </remarks>
    public class InMemoryPartidaRepository : IPartidaRepository
    {
        private readonly List<Partida> _partidas;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<InMemoryPartidaRepository> _logger;

        /// <summary>Crea el repositorio en memoria, inicialmente vacío.</summary>
        /// <param name="logger">Registro de trazas de las operaciones del repositorio.</param>
        public InMemoryPartidaRepository(ILogger<InMemoryPartidaRepository> logger)
        {
            _partidas = new List<Partida>();
            _semaphore = new SemaphoreSlim(1, 1);
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Partida> CrearAsync(Partida partida)
        {
            await _semaphore.WaitAsync();
            try
            {
                _partidas.Add(partida);
                _logger.LogInformation($"Partida creada: {partida.Id}");
                return partida;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<Partida?> ObtenerPorIdAsync(string partidaId)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _partidas.FirstOrDefault(p => p.Id == partidaId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<Partida> ActualizarAsync(Partida partida)
        {
            await _semaphore.WaitAsync();
            try
            {
                var partidaExistente = _partidas.FirstOrDefault(p => p.Id == partida.Id);
                if (partidaExistente != null)
                {
                    var indice = _partidas.IndexOf(partidaExistente);
                    _partidas[indice] = partida;
                    _logger.LogInformation($"Partida actualizada: {partida.Id}");
                }
                return partida;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task EliminarAsync(string partidaId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var partida = _partidas.FirstOrDefault(p => p.Id == partidaId);
                if (partida != null)
                {
                    _partidas.Remove(partida);
                    _logger.LogInformation($"Partida eliminada: {partidaId}");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<List<Partida>> ObtenerTodasAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return new List<Partida>(_partidas);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
