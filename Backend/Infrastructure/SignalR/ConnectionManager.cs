using ServidorAjedrez.Domain.Interfaces;
using System.Collections.Concurrent;

namespace ServidorAjedrez.Infrastructure.SignalR
{
    /// <inheritdoc cref="IConnectionManager"/>
    /// <remarks>
    /// Mantiene dos diccionarios concurrentes (conexión → sala y conexión → nombre), por lo que es
    /// seguro frente a accesos simultáneos de varias conexiones. Es un almacén en memoria pensado
    /// para registrarse como <c>Singleton</c>.
    /// </remarks>
    public class ConnectionManager : IConnectionManager
    {
        private readonly ConcurrentDictionary<string, string> _connectionToSala = new();
        private readonly ConcurrentDictionary<string, string> _connectionToNombre = new();

        /// <inheritdoc/>
        public void AsociarConnectionASala(string connectionId, string salaId)
        {
            _connectionToSala[connectionId] = salaId;
        }

        /// <inheritdoc/>
        public void RemoverConnection(string connectionId)
        {
            _connectionToSala.TryRemove(connectionId, out _);
            _connectionToNombre.TryRemove(connectionId, out _);
        }

        /// <inheritdoc/>
        public string? ObtenerSalaPorConnection(string connectionId)
        {
            return _connectionToSala.TryGetValue(connectionId, out var salaId) ? salaId : null;
        }

        /// <inheritdoc/>
        public void AsociarNombreJugador(string connectionId, string nombreJugador)
        {
            _connectionToNombre[connectionId] = nombreJugador;
        }

        /// <inheritdoc/>
        public string? ObtenerNombrePorConnection(string connectionId)
        {
            return _connectionToNombre.TryGetValue(connectionId, out var nombre) ? nombre : null;
        }

        /// <inheritdoc/>
        public void RemoverNombreJugador(string connectionId)
        {
            _connectionToNombre.TryRemove(connectionId, out _);
        }
    }
}
