using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.Entities
{
    /// <summary>
    /// Jugador participante en una sala o partida.
    /// </summary>
    /// <remarks>
    /// Maneja dos identificadores con propósitos distintos: <see cref="Id"/> es la identidad
    /// estable del jugador dentro del dominio, mientras que <see cref="ConnectionId"/> es el
    /// identificador de transporte de SignalR (puede cambiar al reconectar). Buena parte del
    /// flujo del servidor traduce el connectionId entrante al <see cref="Id"/> de dominio. El
    /// color permanece sin asignar hasta que la partida reparte los bandos.
    /// </remarks>
    public class Jugador
    {
        private string _id;
        private string _nombre;
        private string _connectionId;
        private Color? _color;

        /// <summary>Identidad estable del jugador en el dominio (GUID generado al crearlo).</summary>
        public string Id => _id;

        /// <summary>Nombre visible elegido por el jugador.</summary>
        public string Nombre => _nombre;

        /// <summary>
        /// Identificador de la conexión SignalR del jugador, usado para enrutar mensajes y para
        /// resolver su <see cref="Id"/>. Puede cambiar si el cliente se reconecta.
        /// </summary>
        public string ConnectionId => _connectionId;

        /// <summary>Bando asignado al jugador, o <c>null</c> mientras la partida no lo haya repartido.</summary>
        public Color? Color => _color;

        /// <summary>
        /// Crea un jugador con identidad nueva y sin color asignado.
        /// </summary>
        /// <param name="nombre">Nombre visible; no puede ser nulo ni estar en blanco.</param>
        /// <param name="connectionId">Identificador de la conexión SignalR; no puede ser nulo ni estar en blanco.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="nombre"/> o <paramref name="connectionId"/> son nulos o están en blanco.
        /// </exception>
        public Jugador(string nombre, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del jugador no puede estar vacío.");
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("El connectionId no puede estar vacío.");

            _id = Guid.NewGuid().ToString();
            _nombre = nombre;
            _connectionId = connectionId;
            _color = null;
        }

        /// <summary>Asigna el bando con el que el jugador disputa la partida.</summary>
        /// <param name="color">Bando que se adjudica al jugador.</param>
        public void AsignarColor(Color color)
        {
            _color = color;
        }

        /// <summary>
        /// Crea un jugador. Equivale al constructor; se ofrece como método de fábrica por
        /// consistencia con el resto de entidades del dominio.
        /// </summary>
        /// <param name="nombre">Nombre visible; no puede ser nulo ni estar en blanco.</param>
        /// <param name="connectionId">Identificador de la conexión SignalR; no puede ser nulo ni estar en blanco.</param>
        /// <returns>La nueva instancia de <see cref="Jugador"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="nombre"/> o <paramref name="connectionId"/> son nulos o están en blanco.
        /// </exception>
        public static Jugador Create(string nombre, string connectionId)
        {
            return new Jugador(nombre, connectionId);
        }
    }
}
