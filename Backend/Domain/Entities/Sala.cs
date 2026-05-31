using ServidorAjedrez.Domain.Enums;

namespace ServidorAjedrez.Domain.Entities
{
    /// <summary>
    /// Sala (lobby) que empareja a dos jugadores: un creador y un oponente que se une por nombre.
    /// </summary>
    /// <remarks>
    /// Se localiza por su <see cref="Nombre"/>, que es la clave con la que un segundo jugador la
    /// encuentra para unirse. Al completarse arranca una partida y guarda su id en
    /// <see cref="PartidaActualId"/>; ese id puede reutilizarse en sucesivas revanchas.
    /// </remarks>
    public class Sala
    {
        private string _id;
        private string _nombre;
        private Jugador _creador;
        private Jugador? _oponente;
        private EstadoSala _estado;
        private string? _partidaActualId;

        /// <summary>Identidad estable de la sala (GUID generado al crearla).</summary>
        public string Id => _id;

        /// <summary>Nombre de la sala; sirve de clave para que un oponente la localice y se una.</summary>
        public string Nombre => _nombre;

        /// <summary>Jugador que creó la sala.</summary>
        public Jugador Creador => _creador;

        /// <summary>Oponente que se ha unido, o <c>null</c> mientras la sala sigue esperando rival.</summary>
        public Jugador? Oponente => _oponente;

        /// <summary>Fase del ciclo de vida de la sala.</summary>
        public EstadoSala Estado => _estado;

        /// <summary>Id de la partida en curso (o de la última disputada), o <c>null</c> si aún no se ha iniciado ninguna.</summary>
        public string? PartidaActualId => _partidaActualId;

        /// <summary>
        /// Crea una sala en estado <see cref="EstadoSala.Esperando"/>, con un creador y sin oponente.
        /// </summary>
        /// <param name="nombre">Nombre de la sala; no puede ser nulo ni estar en blanco.</param>
        /// <param name="creador">Jugador que crea la sala; no puede ser nulo.</param>
        /// <exception cref="ArgumentException"><paramref name="nombre"/> es nulo o está en blanco.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="creador"/> es nulo.</exception>
        public Sala(string nombre, Jugador creador)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre de la sala no puede estar vacío.");
            if (creador == null)
                throw new ArgumentNullException(nameof(creador));

            _id = Guid.NewGuid().ToString();
            _nombre = nombre;
            _creador = creador;
            _oponente = null;
            _estado = EstadoSala.Esperando;
            _partidaActualId = null;
        }

        /// <summary>
        /// Asigna el oponente a una sala que aún no lo tiene.
        /// </summary>
        /// <param name="oponente">Jugador que se une como rival; no puede ser nulo.</param>
        /// <exception cref="ArgumentNullException"><paramref name="oponente"/> es nulo.</exception>
        /// <exception cref="InvalidOperationException">La sala ya tiene un oponente asignado.</exception>
        public void AgregarOponente(Jugador oponente)
        {
            if (oponente == null)
                throw new ArgumentNullException(nameof(oponente));
            if (_oponente != null)
                throw new InvalidOperationException("La sala ya tiene un oponente.");

            _oponente = oponente;
        }

        /// <summary>
        /// Vincula la sala a una partida recién creada y la pasa a <see cref="EstadoSala.EnCurso"/>.
        /// </summary>
        /// <param name="partidaId">Id de la partida que se disputa en la sala; no puede ser nulo ni estar en blanco.</param>
        /// <exception cref="ArgumentException"><paramref name="partidaId"/> es nulo o está en blanco.</exception>
        public void IniciarPartida(string partidaId)
        {
            if (string.IsNullOrWhiteSpace(partidaId))
                throw new ArgumentException("El ID de la partida no puede estar vacío.");

            _estado = EstadoSala.EnCurso;
            _partidaActualId = partidaId;
        }

        /// <summary>Marca la sala como <see cref="EstadoSala.Finalizada"/> al terminar su partida.</summary>
        public void FinalizarPartida()
        {
            _estado = EstadoSala.Finalizada;
        }

        /// <summary>Indica si la sala ya tiene a sus dos jugadores (creador y oponente).</summary>
        /// <returns><c>true</c> si hay creador y oponente; en caso contrario, <c>false</c>.</returns>
        public bool EstaCompleta()
        {
            return _creador != null && _oponente != null;
        }

        /// <summary>Comprueba la invariante de la sala: su nombre no está vacío.</summary>
        /// <exception cref="InvalidOperationException">El nombre de la sala es nulo o está en blanco.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_nombre))
                throw new InvalidOperationException("Nombre de sala inválido.");
        }

        /// <summary>Crea una sala. Método de fábrica equivalente al constructor.</summary>
        /// <param name="nombre">Nombre de la sala; no puede ser nulo ni estar en blanco.</param>
        /// <param name="creador">Jugador que crea la sala; no puede ser nulo.</param>
        /// <returns>La nueva instancia de <see cref="Sala"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="nombre"/> es nulo o está en blanco.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="creador"/> es nulo.</exception>
        public static Sala Create(string nombre, Jugador creador)
        {
            return new Sala(nombre, creador);
        }
    }
}
