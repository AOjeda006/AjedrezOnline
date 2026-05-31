using ServidorAjedrez.Domain.ValueObjects;

namespace ServidorAjedrez.Domain.Entities
{
    /// <summary>
    /// Movimiento de una pieza entre dos casillas, con los metadatos necesarios para aplicarlo
    /// y, en su caso, deshacerlo (captura, enroque, promoción).
    /// </summary>
    /// <remarks>
    /// Sigue un esquema de confirmación en dos fases: al crearse queda como tentativo
    /// (<see cref="Confirmado"/> es <c>false</c>) y no consolida el turno hasta llamar a
    /// <see cref="Confirmar"/>; entretanto puede deshacerse. Las marcas <see cref="EsEnroque"/>
    /// y <see cref="EsPromocion"/> llegan del cliente como pistas; el servidor recalcula los
    /// efectos reales (captura, desplazamiento de la torre, coronación).
    /// </remarks>
    public class Movimiento
    {
        private string _id;
        private string _piezaId;
        private Posicion _origen;
        private Posicion _destino;
        private string? _piezaCapturada;
        private bool _esEnroque;
        private bool _esPromocion;
        private bool _confirmado;

        public string Id => _id;
        public string PiezaId => _piezaId;
        public Posicion Origen => _origen;
        public Posicion Destino => _destino;

        /// <summary>Id de la pieza capturada por este movimiento, o <c>null</c> si no hubo captura.</summary>
        public string? PiezaCapturada => _piezaCapturada;

        /// <summary>Indica si el movimiento es un enroque (el rey se desplaza dos columnas).</summary>
        public bool EsEnroque => _esEnroque;

        /// <summary>Indica si el movimiento lleva un peón a la última fila y exige promoción.</summary>
        public bool EsPromocion => _esPromocion;

        /// <summary>
        /// Indica si el movimiento ya se confirmó (y consolidó el cambio de turno). Mientras
        /// es <c>false</c>, el movimiento es tentativo y puede deshacerse.
        /// </summary>
        public bool Confirmado => _confirmado;

        /// <summary>
        /// Crea un movimiento tentativo (sin confirmar) entre dos casillas válidas.
        /// </summary>
        /// <param name="piezaId">Id de la pieza que se mueve; no puede ser nulo ni estar en blanco.</param>
        /// <param name="origen">Casilla de origen; debe estar dentro del tablero.</param>
        /// <param name="destino">Casilla de destino; debe estar dentro del tablero.</param>
        /// <param name="piezaCapturada">Id de la pieza capturada, o <c>null</c> si no hay captura.</param>
        /// <param name="esEnroque">Marca el movimiento como enroque.</param>
        /// <param name="esPromocion">Marca que el movimiento desemboca en promoción.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="piezaId"/> es nulo o está en blanco, o <paramref name="origen"/> o
        /// <paramref name="destino"/> caen fuera del tablero.
        /// </exception>
        public Movimiento(string piezaId, Posicion origen, Posicion destino, string? piezaCapturada = null, bool esEnroque = false, bool esPromocion = false)
        {
            if (string.IsNullOrWhiteSpace(piezaId))
                throw new ArgumentException("El ID de la pieza no puede estar vacío.");
            if (!origen.EsValida() || !destino.EsValida())
                throw new ArgumentException("Posiciones inválidas para el movimiento.");

            _id = Guid.NewGuid().ToString();
            _piezaId = piezaId;
            _origen = origen;
            _destino = destino;
            _piezaCapturada = piezaCapturada;
            _esEnroque = esEnroque;
            _esPromocion = esPromocion;
            _confirmado = false;
        }

        /// <summary>
        /// Marca el movimiento como confirmado. Lo invoca la partida al consolidar el turno.
        /// </summary>
        /// <seealso cref="Confirmado"/>
        public void Confirmar()
        {
            _confirmado = true;
        }

        /// <summary>
        /// Comprueba la invariante del movimiento: origen y destino siguen siendo casillas
        /// dentro del tablero.
        /// </summary>
        /// <exception cref="InvalidOperationException">El origen o el destino están fuera del tablero.</exception>
        public void Validate()
        {
            if (!_origen.EsValida() || !_destino.EsValida())
                throw new InvalidOperationException("Posiciones del movimiento inválidas.");
        }

        /// <summary>Crea un movimiento. Método de fábrica equivalente al constructor.</summary>
        /// <param name="piezaId">Id de la pieza que se mueve; no puede ser nulo ni estar en blanco.</param>
        /// <param name="origen">Casilla de origen; debe estar dentro del tablero.</param>
        /// <param name="destino">Casilla de destino; debe estar dentro del tablero.</param>
        /// <param name="piezaCapturada">Id de la pieza capturada, o <c>null</c> si no hay captura.</param>
        /// <param name="esEnroque">Marca el movimiento como enroque.</param>
        /// <param name="esPromocion">Marca que el movimiento desemboca en promoción.</param>
        /// <returns>La nueva instancia de <see cref="Movimiento"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="piezaId"/> es nulo o está en blanco, o <paramref name="origen"/> o
        /// <paramref name="destino"/> caen fuera del tablero.
        /// </exception>
        public static Movimiento Create(string piezaId, Posicion origen, Posicion destino, string? piezaCapturada = null, bool esEnroque = false, bool esPromocion = false)
        {
            return new Movimiento(piezaId, origen, destino, piezaCapturada, esEnroque, esPromocion);
        }
    }
}
