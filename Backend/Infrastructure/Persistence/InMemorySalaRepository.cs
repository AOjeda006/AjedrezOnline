using Microsoft.Extensions.Logging;
using ServidorAjedrez.Domain.Entities;
using ServidorAjedrez.Domain.Repositories;

namespace ServidorAjedrez.Infrastructure.Persistence
{
    /// <inheritdoc cref="ISalaRepository"/>
    /// <remarks>
    /// Almacenamiento en memoria, sin persistencia: los datos se pierden al reiniciar el servidor.
    /// Está pensado para registrarse como <c>Singleton</c> y serializa todos los accesos con un
    /// <see cref="System.Threading.SemaphoreSlim"/> para ser seguro ante operaciones concurrentes.
    /// </remarks>
    public class InMemorySalaRepository : ISalaRepository
    {
        private readonly List<Sala> _salas;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<InMemorySalaRepository> _logger;

        /// <summary>Crea el repositorio en memoria, inicialmente vacío.</summary>
        /// <param name="logger">Registro de trazas de las operaciones del repositorio.</param>
        public InMemorySalaRepository(ILogger<InMemorySalaRepository> logger)
        {
            _salas = new List<Sala>();
            _semaphore = new SemaphoreSlim(1, 1);
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Sala> CrearAsync(Sala sala)
        {
            await _semaphore.WaitAsync();
            try
            {
                _salas.Add(sala);
                _logger.LogInformation($"Sala creada: {sala.Id}");
                return sala;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<Sala?> ObtenerPorIdAsync(string salaId)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _salas.FirstOrDefault(s => s.Id == salaId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<Sala?> ObtenerPorNombreAsync(string nombre)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _salas.FirstOrDefault(s => s.Nombre == nombre && s.Oponente == null);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<Sala> ActualizarAsync(Sala sala)
        {
            await _semaphore.WaitAsync();
            try
            {
                var salaExistente = _salas.FirstOrDefault(s => s.Id == sala.Id);
                if (salaExistente != null)
                {
                    var indice = _salas.IndexOf(salaExistente);
                    _salas[indice] = sala;
                    _logger.LogInformation($"Sala actualizada: {sala.Id}");
                }
                return sala;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task EliminarAsync(string salaId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var sala = _salas.FirstOrDefault(s => s.Id == salaId);
                if (sala != null)
                {
                    _salas.Remove(sala);
                    _logger.LogInformation($"Sala eliminada: {salaId}");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<List<Sala>> ObtenerTodasAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return new List<Sala>(_salas);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
