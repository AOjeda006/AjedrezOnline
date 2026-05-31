using System.Collections.Generic;

namespace ServidorAjedrez.Domain.Interfaces
{
    /// <summary>
    /// Registro en memoria que asocia cada conexión SignalR con la sala en la que está y con el
    /// nombre del jugador, fuera del ciclo de vida de las entidades de dominio.
    /// </summary>
    /// <remarks>
    /// Lo usa el hub para, a partir de la conexión actual, resolver la sala (y de ahí la partida) y
    /// recuperar el nombre fijado antes de crear o unirse a una sala. Las implementaciones deben ser
    /// seguras frente a accesos concurrentes, ya que varias conexiones operan en paralelo.
    /// </remarks>
    public interface IConnectionManager
    {
        /// <summary>Asocia una conexión con la sala en la que participa (sobrescribe la asociación previa, si la hubiera).</summary>
        /// <param name="connectionId">Identificador de la conexión.</param>
        /// <param name="salaId">Identificador de la sala.</param>
        void AsociarConnectionASala(string connectionId, string salaId);

        /// <summary>Elimina toda asociación de la conexión (sala y nombre). Idempotente si no existe.</summary>
        /// <param name="connectionId">Identificador de la conexión a olvidar.</param>
        void RemoverConnection(string connectionId);

        /// <summary>Devuelve la sala asociada a una conexión.</summary>
        /// <param name="connectionId">Identificador de la conexión.</param>
        /// <returns>El id de la sala, o <c>null</c> si la conexión no está asociada a ninguna.</returns>
        string? ObtenerSalaPorConnection(string connectionId);

        /// <summary>Asocia el nombre del jugador a una conexión (lo fija el cliente antes de crear o unirse a una sala).</summary>
        /// <param name="connectionId">Identificador de la conexión.</param>
        /// <param name="nombreJugador">Nombre elegido por el jugador.</param>
        void AsociarNombreJugador(string connectionId, string nombreJugador);

        /// <summary>Devuelve el nombre del jugador asociado a una conexión.</summary>
        /// <param name="connectionId">Identificador de la conexión.</param>
        /// <returns>El nombre del jugador, o <c>null</c> si la conexión no tiene nombre asociado.</returns>
        string? ObtenerNombrePorConnection(string connectionId);

        /// <summary>Elimina solo la asociación de nombre de la conexión, conservando la de sala.</summary>
        /// <param name="connectionId">Identificador de la conexión.</param>
        void RemoverNombreJugador(string connectionId);
    }
}
