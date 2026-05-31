using ServidorAjedrez.Domain.Enums;
using ServidorAjedrez.Domain.ValueObjects;

namespace ServidorAjedrez.Domain.Entities
{
    /// <summary>
    /// Pieza de ajedrez, con su tipo, color, posición y estado dentro de la partida.
    /// </summary>
    /// <remarks>
    /// Las piezas capturadas no se quitan de la colección del tablero: se marcan con
    /// <see cref="Eliminada"/> (borrado lógico), lo que permite restaurarlas al deshacer un
    /// movimiento. El indicador <see cref="SeHaMovido"/> se trata con cuidado porque de él
    /// dependen el derecho de enroque y el avance doble inicial del peón; por eso al revertir se
    /// usa <see cref="RevertirA"/> en lugar de <see cref="Mover"/>.
    /// </remarks>
    public class Pieza
    {
        private string _id;
        private TipoPieza _tipo;
        private Color _color;
        private Posicion _posicion;
        private bool _eliminada;
        private bool _seHaMovido;

        /// <summary>Identidad estable de la pieza (GUID generado al crearla).</summary>
        public string Id => _id;

        public TipoPieza Tipo => _tipo;
        public Color Color => _color;
        public Posicion Posicion => _posicion;

        /// <summary>Indica si la pieza ha sido capturada (borrado lógico: permanece en la colección, pero fuera de juego).</summary>
        public bool Eliminada => _eliminada;

        /// <summary>
        /// Indica si la pieza se ha movido alguna vez. Determina el derecho de enroque (rey y
        /// torre) y condiciona el avance doble inicial del peón.
        /// </summary>
        public bool SeHaMovido => _seHaMovido;

        /// <summary>Crea una pieza en la casilla indicada, no capturada y sin haberse movido.</summary>
        /// <param name="tipo">Tipo de la pieza.</param>
        /// <param name="color">Bando al que pertenece.</param>
        /// <param name="posicion">Casilla inicial; debe estar dentro del tablero.</param>
        /// <exception cref="ArgumentException"><paramref name="posicion"/> cae fuera del tablero.</exception>
        public Pieza(TipoPieza tipo, Color color, Posicion posicion)
        {
            if (!posicion.EsValida())
                throw new ArgumentException("Posición inválida para la pieza.");

            _id = Guid.NewGuid().ToString();
            _tipo = tipo;
            _color = color;
            _posicion = posicion;
            _eliminada = false;
            _seHaMovido = false;
        }

        /// <summary>
        /// Mueve la pieza a una nueva casilla y marca que ya se ha movido.
        /// </summary>
        /// <remarks>
        /// No valida la legalidad ajedrecística del movimiento (eso compete a
        /// <see cref="Tablero.EsMovimientoValido"/>); solo comprueba que el destino esté dentro
        /// del tablero. Para revertir un movimiento sin perder el estado de enroque, usa
        /// <see cref="RevertirA"/>.
        /// </remarks>
        /// <param name="nuevaPosicion">Casilla de destino; debe estar dentro del tablero.</param>
        /// <exception cref="ArgumentException"><paramref name="nuevaPosicion"/> cae fuera del tablero.</exception>
        public void Mover(Posicion nuevaPosicion)
        {
            if (!nuevaPosicion.EsValida())
                throw new ArgumentException("Nueva posición inválida.");

            _posicion = nuevaPosicion;
            _seHaMovido = true;
        }

        /// <summary>
        /// Restaura la posición y el indicador <see cref="SeHaMovido"/> a un estado anterior.
        /// </summary>
        /// <remarks>
        /// Se usa al simular movimientos (detección de jaque) y al deshacer, para no corromper el
        /// derecho de enroque: a diferencia de <see cref="Mover"/>, fija <see cref="SeHaMovido"/>
        /// al valor recibido en lugar de forzarlo a <c>true</c>.
        /// </remarks>
        /// <param name="posicion">Casilla a la que se restituye la pieza; debe estar dentro del tablero.</param>
        /// <param name="seHaMovido">Valor de <see cref="SeHaMovido"/> previo al movimiento que se revierte.</param>
        /// <exception cref="ArgumentException"><paramref name="posicion"/> cae fuera del tablero.</exception>
        public void RevertirA(Posicion posicion, bool seHaMovido)
        {
            if (!posicion.EsValida())
                throw new ArgumentException("Posición inválida al revertir el movimiento.");

            _posicion = posicion;
            _seHaMovido = seHaMovido;
        }

        /// <summary>Marca la pieza como capturada (borrado lógico). Reversible con <see cref="Restaurar"/>.</summary>
        public void Eliminar()
        {
            _eliminada = true;
        }

        /// <summary>Vuelve a poner la pieza en juego. Operación inversa de <see cref="Eliminar"/>, usada al deshacer una captura.</summary>
        public void Restaurar()
        {
            _eliminada = false;
        }

        /// <summary>
        /// Corona el peón sustituyendo su tipo por el indicado.
        /// </summary>
        /// <param name="nuevoTipo">Tipo al que se promociona; debe ser torre, caballo, alfil o reina.</param>
        /// <exception cref="InvalidOperationException">
        /// La pieza no es un peón, o <paramref name="nuevoTipo"/> es
        /// <see cref="TipoPieza.Peon"/> o <see cref="TipoPieza.Rey"/> (destinos no permitidos).
        /// </exception>
        public void Promocionar(TipoPieza nuevoTipo)
        {
            if (_tipo != TipoPieza.Peon)
                throw new InvalidOperationException("Solo los peones pueden ser promocionados.");
            if (nuevoTipo == TipoPieza.Peon || nuevoTipo == TipoPieza.Rey)
                throw new InvalidOperationException("No se puede promocionar a peón o rey.");

            _tipo = nuevoTipo;
        }

        /// <summary>Comprueba la invariante de la pieza: su posición está dentro del tablero.</summary>
        /// <exception cref="InvalidOperationException">La posición de la pieza está fuera del tablero.</exception>
        public void Validate()
        {
            if (!_posicion.EsValida())
                throw new InvalidOperationException("Posición de la pieza inválida.");
        }

        /// <summary>Crea una pieza. Método de fábrica equivalente al constructor.</summary>
        /// <param name="tipo">Tipo de la pieza.</param>
        /// <param name="color">Bando al que pertenece.</param>
        /// <param name="posicion">Casilla inicial; debe estar dentro del tablero.</param>
        /// <returns>La nueva instancia de <see cref="Pieza"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="posicion"/> cae fuera del tablero.</exception>
        public static Pieza Create(TipoPieza tipo, Color color, Posicion posicion)
        {
            return new Pieza(tipo, color, posicion);
        }
    }
}
